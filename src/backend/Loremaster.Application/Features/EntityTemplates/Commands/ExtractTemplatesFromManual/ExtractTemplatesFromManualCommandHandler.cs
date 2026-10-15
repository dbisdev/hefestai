using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityTemplates.Commands.ExtractTemplatesFromManual.Services;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityTemplates.Commands.ExtractTemplatesFromManual;

/// <summary>
/// Orchestrates extraction of entity templates from game-manual documents using RAG:
/// <list type="number">
///   <item>Authorises the request and resolves the template owner.</item>
///   <item>Delegates document retrieval and LLM generation to <see cref="IEntityExtractionRagService"/>.</item>
///   <item>Delegates JSON parsing and normalisation to <see cref="IEntityTypeParser"/>.</item>
///   <item>Creates or updates <see cref="EntityTemplate"/> records in the database.</item>
/// </list>
/// All non-trivial business logic (field conversion, schema flattening, name normalisation,
/// RAG search) lives in dedicated single-responsibility services under the
/// <c>Services/</c> subfolder.
/// </summary>
internal class ExtractTemplatesFromManualCommandHandler
    : IRequestHandler<ExtractTemplatesFromManualCommand, ExtractTemplatesResult>
{
    private readonly IEntityTemplateRepository _templateRepository;
    private readonly IGameSystemAuthorizationService _authorizationService;
    private readonly IEntityExtractionRagService _ragService;
    private readonly IEntityTypeParser _entityTypeParser;
    private readonly IFieldDefinitionConverter _fieldConverter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExtractTemplatesFromManualCommandHandler> _logger;

    /// <summary>Initialises the handler with all required dependencies.</summary>
    public ExtractTemplatesFromManualCommandHandler(
        IEntityTemplateRepository templateRepository,
        IGameSystemAuthorizationService authorizationService,
        IEntityExtractionRagService ragService,
        IEntityTypeParser entityTypeParser,
        IFieldDefinitionConverter fieldConverter,
        IUnitOfWork unitOfWork,
        ILogger<ExtractTemplatesFromManualCommandHandler> logger)
    {
        _templateRepository = templateRepository;
        _authorizationService = authorizationService;
        _ragService = ragService;
        _entityTypeParser = entityTypeParser;
        _fieldConverter = fieldConverter;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ExtractTemplatesResult> Handle(
        ExtractTemplatesFromManualCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Extracting templates from manuals for game system {GameSystemId}",
            request.GameSystemId);

        // 1. Authorise — throws ArgumentException (not found) or UnauthorizedAccessException (forbidden).
        var gameSystem = await _authorizationService.RequireOwnerOrAdminAsync(
            request.GameSystemId,
            request.UserId,
            request.IsAdmin,
            action: "extract templates",
            cancellationToken);

        var isSystemOwner = gameSystem.OwnerId == request.UserId;
        var templateOwnerId = gameSystem.OwnerId;

        // 2. Build RAG context from documents.
        var (contextChunks, firstDocumentId) = await _ragService.GetExtractionContextAsync(
            request.GameSystemId,
            request.UserId,
            isAdminOrOwner: request.IsAdmin || isSystemOwner,
            cancellationToken: cancellationToken);

        if (contextChunks.Count == 0)
        {
            return new ExtractTemplatesResult(
                0, 0, 0,
                Array.Empty<ExtractedTemplateInfo>(),
                "No manuals found for this game system. Please upload manuals first.");
        }

        // 3. Call LLM to extract entity types.
        var rawLlmResponse = await _ragService.GenerateEntityTypesAsync(contextChunks, cancellationToken);

        // 4. Parse + normalise + deduplicate.
        var extractedTypes = _entityTypeParser.ParseAndNormalize(rawLlmResponse);

        if (extractedTypes.Count == 0)
        {
            _logger.LogWarning("No entity types could be extracted from the manuals");
            return new ExtractTemplatesResult(
                0, 0, 0,
                Array.Empty<ExtractedTemplateInfo>(),
                "Could not extract entity types from the manuals. The content may not contain clear entity definitions.");
        }

        _logger.LogInformation(
            "Extracted {EntityTypeCount} entity types from manuals", extractedTypes.Count);

        var sourceDocumentId = request.SourceDocumentId ?? firstDocumentId ?? Guid.Empty;

        // 5. Persist extracted types.
        var results = new List<ExtractedTemplateInfo>(extractedTypes.Count);
        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var extractedType in extractedTypes)
        {
            try
            {
                var (template, isNew, wasSkipped, extractedFields) =
                    await CreateOrUpdateTemplateAsync(
                        extractedType,
                        request.GameSystemId,
                        templateOwnerId,
                        sourceDocumentId,
                        cancellationToken);

                if (wasSkipped)
                {
                    skipped++;
                    results.Add(new ExtractedTemplateInfo(
                        template.Id,
                        template.EntityTypeName,
                        template.DisplayName,
                        extractedFields?.Count ?? template.GetFieldDefinitions().Count,
                        false,
                        $"Already confirmed. New extraction has {extractedFields?.Count ?? 0} fields.",
                        extractedFields));
                }
                else
                {
                    if (isNew) created++; else updated++;
                    results.Add(new ExtractedTemplateInfo(
                        template.Id,
                        template.EntityTypeName,
                        template.DisplayName,
                        template.GetFieldDefinitions().Count,
                        isNew,
                        null,
                        null));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to create template for entity type {EntityType}",
                    extractedType.EntityTypeName);

                skipped++;
                results.Add(new ExtractedTemplateInfo(
                    Guid.Empty,
                    extractedType.EntityTypeName,
                    extractedType.DisplayName,
                    0, false, ex.Message, null));
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Template extraction complete: {Created} created, {Updated} updated, {Skipped} skipped",
            created, updated, skipped);

        return new ExtractTemplatesResult(created, updated, skipped, results);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new template, updates an existing draft, or skips (returning existing) if a
    /// confirmed template already exists for the entity type.
    /// </summary>
    /// <returns>
    /// Tuple of (template, isNew, wasSkipped, extractedFields).
    /// <c>extractedFields</c> is non-null only when <c>wasSkipped</c> is <see langword="true"/>
    /// so the caller can present a diff to the user.
    /// </returns>
    private async Task<(EntityTemplate Template, bool IsNew, bool WasSkipped, IReadOnlyList<FieldDefinition>? ExtractedFields)>
        CreateOrUpdateTemplateAsync(
            ExtractedEntityType extractedType,
            Guid gameSystemId,
            Guid ownerId,
            Guid sourceDocumentId,
            CancellationToken cancellationToken)
    {
        var normalizedName = EntityTemplate.NormalizeEntityTypeName(extractedType.EntityTypeName);
        var extractedFields = _fieldConverter.Convert(extractedType.Fields);

        // A confirmed template already exists — skip update but return extracted fields for diff.
        var existingConfirmed = await _templateRepository.GetConfirmedTemplateForEntityTypeAsync(
            gameSystemId, ownerId, normalizedName, cancellationToken);

        if (existingConfirmed is not null)
        {
            _logger.LogInformation(
                "Skipping extraction for '{EntityType}' — confirmed template already exists. " +
                "Extracted {FieldCount} fields for comparison.",
                normalizedName, extractedFields.Count);

            return (existingConfirmed, false, true, extractedFields);
        }

        // Update draft if one exists.
        var allTemplates = await _templateRepository.GetByGameSystemIdAsync(
            gameSystemId, ownerId, cancellationToken);

        var draft = allTemplates.FirstOrDefault(t =>
            t.EntityTypeName == normalizedName && t.Status != TemplateStatus.Confirmed);

        if (draft is not null)
        {
            draft.Update(extractedType.DisplayName, extractedType.Description, extractedType.IconHint);
            draft.SetFieldDefinitions(extractedFields.ToList());
            _templateRepository.Update(draft);
            return (draft, false, false, null);
        }

        // Create new template.
        var newTemplate = EntityTemplate.Create(
            extractedType.EntityTypeName,
            extractedType.DisplayName,
            gameSystemId,
            ownerId,
            extractedType.Description,
            sourceDocumentId,
            iconHint: extractedType.IconHint);

        newTemplate.SetFieldDefinitions(extractedFields.ToList());
        await _templateRepository.AddAsync(newTemplate, cancellationToken);
        return (newTemplate, true, false, null);
    }
}
