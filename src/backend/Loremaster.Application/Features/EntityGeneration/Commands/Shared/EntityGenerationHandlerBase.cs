using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityGeneration.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Loremaster.Application.Features.EntityGeneration.Commands.Shared;

/// <summary>
/// Abstract base class that encapsulates the full entity-generation pipeline shared by all
/// "Generate*" command handlers (character, NPC, vehicle, enemy, encounter, mission, solar system).
///
/// Concrete handlers only need to provide entity-specific data through the abstract members;
/// the pipeline (template resolution → RAG context → JSON generation → image generation →
/// tracking → result) is defined here once.
/// </summary>
/// <typeparam name="TCommand">The MediatR command type.</typeparam>
/// <typeparam name="TResult">The result record type, which must inherit <see cref="EntityGenerationResult"/>.</typeparam>
public abstract class EntityGenerationHandlerBase<TCommand, TResult>
    : IRequestHandler<TCommand, TResult>
    where TCommand : IRequest<TResult>
    where TResult : EntityGenerationResult, new()
{
    // ── Injected services ──────────────────────────────────────────────────────
    private readonly IAiService _aiService;
    private readonly IRagContextProvider _ragContextProvider;
    private readonly IRagGenerationService _ragGeneration;
    private readonly ITemplateResolutionService _templateResolution;
    private readonly IPromptBuilderService _promptBuilder;
    private readonly IImageGenerationService _imageGeneration;
    private readonly IGenerationTrackingService _tracking;

    /// <summary>Logger available to derived classes for entity-specific log messages.</summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Initialises the base handler with all shared service dependencies.
    /// Derived class constructors should call this constructor via <c>base(...)</c>.
    /// </summary>
    protected EntityGenerationHandlerBase(
        IAiService aiService,
        IRagContextProvider ragContextProvider,
        IRagGenerationService ragGeneration,
        ITemplateResolutionService templateResolution,
        IPromptBuilderService promptBuilder,
        IImageGenerationService imageGeneration,
        IGenerationTrackingService tracking,
        ILogger logger)
    {
        _aiService = aiService;
        _ragContextProvider = ragContextProvider;
        _ragGeneration = ragGeneration;
        _templateResolution = templateResolution;
        _promptBuilder = promptBuilder;
        _imageGeneration = imageGeneration;
        _tracking = tracking;
        Logger = logger;
    }

    // ── Abstract template-method hooks ─────────────────────────────────────────

    /// <summary>
    /// Returns the entity type name used for RAG queries, image generation, and generation
    /// tracking (e.g. <c>"character"</c>, <c>"vehicle"</c>, <c>"solar-system"</c>).
    /// </summary>
    protected abstract string GetEntityType(TCommand request);

    /// <summary>
    /// Returns the entity type name passed to <see cref="ITemplateResolutionService.ResolveTemplateAsync"/>.
    /// Override when the template slug differs from the general entity type key
    /// (e.g. <c>"solar_system"</c> vs. <c>"solar-system"</c>).
    /// Defaults to <see cref="GetEntityType"/>.
    /// </summary>
    protected virtual string GetTemplateEntityType(TCommand request) => GetEntityType(request);

    /// <summary>
    /// Returns a human-readable entity label used in log messages (e.g. <c>"Character"</c>).
    /// Defaults to <see cref="GetEntityType"/> with the first letter capitalised.
    /// </summary>
    protected virtual string GetEntityLabel(TCommand request) =>
        char.ToUpperInvariant(GetEntityType(request)[0]) + GetEntityType(request)[1..];

    /// <summary>
    /// Returns the <c>UserId</c> from the command.
    /// </summary>
    protected abstract Guid GetUserId(TCommand request);

    /// <summary>
    /// Returns the optional <c>GameSystemId</c> from the command.
    /// </summary>
    protected abstract Guid? GetGameSystemId(TCommand request);

    /// <summary>
    /// Returns whether the caller requested image generation.
    /// </summary>
    protected abstract bool ShouldGenerateImage(TCommand request);

    /// <summary>
    /// Builds the free-text query string used for the RAG (retrieval-augmented generation)
    /// context lookup, summarising the entity's key attributes.
    /// Returns <see langword="null"/> when RAG should be skipped.
    /// </summary>
    protected abstract string? BuildRagQuery(TCommand request);

    /// <summary>
    /// Returns the AI system prompt used when RAG context is available.
    /// </summary>
    protected abstract string GetSystemPrompt(TCommand request);

    /// <summary>
    /// Returns the AI user query (with field descriptions and example JSON injected)
    /// used when RAG context is available.
    /// </summary>
    protected abstract string BuildUserQuery(TCommand request, string fieldDescriptions, string exampleJson);

    /// <summary>
    /// Returns the AI system prompt used when no RAG context is available.
    /// </summary>
    protected abstract string GetFallbackSystemPrompt(TCommand request);

    /// <summary>
    /// Returns the AI user prompt (with field descriptions and example JSON injected)
    /// used when no RAG context is available.
    /// </summary>
    protected abstract string BuildFallbackPrompt(TCommand request, string fieldDescriptions, string exampleJson);

    /// <summary>
    /// Builds the <see cref="ImageGenerationContext"/> passed to the image generation service.
    /// Return <see langword="null"/> to skip image generation even when
    /// <see cref="ShouldGenerateImage"/> returns <see langword="true"/>.
    /// </summary>
    protected abstract ImageGenerationContext? BuildImageContext(
        TCommand request, string? entityJson);

    /// <summary>
    /// Constructs the strongly-typed result record from the generation outputs.
    /// The base implementation populates all common fields; override only when
    /// the concrete result type adds extra properties.
    /// </summary>
    protected virtual TResult BuildResult(
        string? entityJson,
        string? imageBase64,
        string? imageUrl,
        bool ragContextUsed,
        int ragSourceCount,
        Guid? generationRequestId) =>
        new()
        {
            Success = true,
            EntityJson = entityJson,
            ImageBase64 = imageBase64,
            ImageUrl = imageUrl,
            RagContextUsed = ragContextUsed,
            RagSourceCount = ragSourceCount,
            GenerationRequestId = generationRequestId
        };

    /// <summary>
    /// Constructs a failure result. Override if the concrete type adds extra properties
    /// and you need to populate them on failure as well.
    /// </summary>
    protected virtual TResult BuildErrorResult(Exception ex) =>
        new() { Success = false, Error = ex.Message };

    // ── MediatR pipeline entry-point ───────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<TResult> Handle(TCommand request, CancellationToken cancellationToken)
    {
        var entityType = GetEntityType(request);
        var entityLabel = GetEntityLabel(request);

        try
        {
            Logger.LogInformation("Generating {EntityLabel} (type={EntityType})", entityLabel, entityType);

            // 1. Resolve entity template (returns example JSON + field descriptions)
            var templateResult = await _templateResolution.ResolveTemplateAsync(
                GetGameSystemId(request), GetUserId(request), GetTemplateEntityType(request), entityLabel, cancellationToken);

            var (exampleJson, fieldDescriptions) = templateResult.HasTemplate
                ? (templateResult.ExampleJson, templateResult.FieldDescriptions)
                : (templateResult.FallbackExampleJson, templateResult.FallbackFieldDescriptions);

            // 2. Retrieve RAG context
            var ragContext = await GetRagContextAsync(request, entityType, cancellationToken);

            // 3. Generate entity JSON (RAG-augmented or fallback)
            var (entityJson, fullPrompt) = await GenerateJsonAsync(
                request, ragContext, exampleJson, fieldDescriptions, cancellationToken);

            // 4. Optionally generate image
            string? imageBase64 = null;
            string? imageUrl = null;

            if (ShouldGenerateImage(request))
            {
                var imageContext = BuildImageContext(request, entityJson);
                if (imageContext is not null)
                {
                    var imageResult = await _imageGeneration.GenerateEntityImageAsync(
                        imageContext, cancellationToken);
                    imageBase64 = imageResult.ImageBase64;
                    imageUrl = imageResult.ImageUrl;
                }
            }

            // 5. Track generation request + result
            var generationRequest = _tracking.CreateRequest(GetUserId(request), entityType, fullPrompt);
            generationRequest.Complete();
            var generationResult = _tracking.CreateResult(
                generationRequest.Id,
                entityJson,
                ragContext.Any(),
                ragContext.Count,
                imageBase64 != null || imageUrl != null);

            await _tracking.PersistAsync(generationRequest, generationResult, cancellationToken);

            Logger.LogInformation(
                "{EntityLabel} generation completed. GenerationRequestId={RequestId}",
                entityLabel, generationRequest.Id);

            return BuildResult(entityJson, imageBase64, imageUrl, ragContext.Any(), ragContext.Count, generationRequest.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate {EntityLabel}", entityLabel);
            return BuildErrorResult(ex);
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves RAG context chunks for the given command.
    /// Returns an empty list when no <c>GameSystemId</c> is present or the query is null.
    /// </summary>
    private async Task<IReadOnlyList<RagContextChunk>> GetRagContextAsync(
        TCommand request,
        string entityType,
        CancellationToken ct)
    {
        var gameSystemId = GetGameSystemId(request);
        var ragQuery = BuildRagQuery(request);

        if (!gameSystemId.HasValue || ragQuery is null)
            return Array.Empty<RagContextChunk>();

        return await _ragContextProvider.GetContextForEntityGenerationAsync(
            gameSystemId.Value, GetUserId(request), entityType, ragQuery, 7, ct);
    }

    /// <summary>
    /// Generates the entity JSON using either RAG-augmented generation (when context chunks
    /// are available) or a direct AI call as fallback.
    /// </summary>
    private async Task<(string Json, string FullPrompt)> GenerateJsonAsync(
        TCommand request,
        IReadOnlyList<RagContextChunk> ragContext,
        string exampleJson,
        string fieldDescriptions,
        CancellationToken ct)
    {
        if (ragContext.Any())
        {
            var systemPrompt = GetSystemPrompt(request);
            var userQuery = BuildUserQuery(request, fieldDescriptions, exampleJson);
            var contextTexts = ragContext.Select(c => c.Content).ToList();
            var fullPrompt = _promptBuilder.BuildFullPromptTrace(systemPrompt, userQuery, contextTexts);

            var result = await _ragGeneration.GenerateWithContextAsync(
                userQuery, contextTexts, systemPrompt,
                DefaultGenerationConstants.Temperature,
                DefaultGenerationConstants.MaxTokens,
                ct);

            return (result.Answer, fullPrompt);
        }
        else
        {
            var fallbackSystem = GetFallbackSystemPrompt(request);
            var fallbackPrompt = BuildFallbackPrompt(request, fieldDescriptions, exampleJson);
            var fullPrompt = _promptBuilder.BuildFullPromptTrace(fallbackSystem, fallbackPrompt);

            var result = await _aiService.GenerateJsonAsync(
                fallbackPrompt, fallbackSystem,
                DefaultGenerationConstants.Temperature,
                DefaultGenerationConstants.MaxTokens,
                ct);

            return (result.Json, fullPrompt);
        }
    }

    // ── Shared utility ─────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to extract the <c>description</c> property from a generated entity JSON string.
    /// Returns <see langword="null"/> if the JSON is empty, unparsable, or lacks the property.
    /// </summary>
    protected static string? ExtractDescription(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            var cleaned = Loremaster.Shared.Helpers.JsonSanitizationHelper.StripMarkdownCodeFences(json);
            using var doc = JsonDocument.Parse(cleaned);
            if (doc.RootElement.TryGetProperty("description", out var desc))
                return desc.GetString();
        }
        catch { /* malformed JSON — silently skip */ }
        return null;
    }
}
