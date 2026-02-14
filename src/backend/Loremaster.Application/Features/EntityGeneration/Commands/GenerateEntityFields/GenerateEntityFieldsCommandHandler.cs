using System.Text.Json;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityGeneration.DTOs;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using Loremaster.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateEntityFields;

/// <summary>
/// Handler for GenerateEntityFieldsCommand.
/// Generates entity field values using RAG and confirmed templates.
/// Core implementation of EPIC 4.5 - Entity Assisted Generation.
/// Creates GenerationRequest and GenerationResult records for traceability.
/// </summary>
public class GenerateEntityFieldsCommandHandler 
    : IRequestHandler<GenerateEntityFieldsCommand, GenerateEntityFieldsResult>
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly IEntityTemplateRepository _entityTemplateRepository;
    private readonly IEntityGenerationService _entityGenerationService;
    private readonly IGenerationRequestRepository _generationRequestRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GenerateEntityFieldsCommandHandler> _logger;

    /// <summary>
    /// Default model name for Gemini 2.0 Flash used in generation.
    /// </summary>
    private const string DefaultModelName = "gemini-2.0-flash";

    public GenerateEntityFieldsCommandHandler(
        ICampaignRepository campaignRepository,
        ICampaignMemberRepository campaignMemberRepository,
        IEntityTemplateRepository entityTemplateRepository,
        IEntityGenerationService entityGenerationService,
        IGenerationRequestRepository generationRequestRepository,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<GenerateEntityFieldsCommandHandler> logger)
    {
        _campaignRepository = campaignRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _entityTemplateRepository = entityTemplateRepository;
        _entityGenerationService = entityGenerationService;
        _generationRequestRepository = generationRequestRepository;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the generate entity fields command.
    /// Creates GenerationRequest and GenerationResult records for traceability.
    /// </summary>
    public async Task<GenerateEntityFieldsResult> Handle(
        GenerateEntityFieldsCommand request, 
        CancellationToken cancellationToken)
    {
        // Validate authentication
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to generate entity fields");
        }

        var userId = _currentUserService.UserId.Value;

        // Validate campaign membership
        var membership = await _campaignMemberRepository
            .GetByCampaignAndUserAsync(request.CampaignId, userId, cancellationToken);
        
        if (membership == null)
        {
            throw new NotFoundException("Campaign", request.CampaignId);
        }

        // Get campaign for game system context
        var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign == null)
        {
            throw new NotFoundException("Campaign", request.CampaignId);
        }

        // Normalize entity type and find confirmed template
        var normalizedEntityType = EntityTemplate.NormalizeEntityTypeName(request.EntityTypeName);
        
        // Templates are either owned by the campaign's Master (OwnerId) or by an Admin (shared globally)
        var template = await _entityTemplateRepository.GetConfirmedTemplateForEntityTypeAsync(
            campaign.GameSystemId,
            campaign.OwnerId,
            normalizedEntityType,
            cancellationToken);

        if (template == null)
        {
            _logger.LogWarning(
                "Entity generation rejected: No confirmed template for type '{EntityType}' in game system {GameSystemId}",
                normalizedEntityType, campaign.GameSystemId);
            
            return GenerateEntityFieldsResult.Failed(
                $"No confirmed template found for entity type '{request.EntityTypeName}'. " +
                "A confirmed template must exist before entities of this type can be generated.",
                entityTypeName: normalizedEntityType);
        }

        _logger.LogInformation(
            "Starting entity field generation for type '{EntityType}' using template {TemplateId} " +
            "in campaign {CampaignId} by user {UserId}",
            normalizedEntityType, template.Id, request.CampaignId, userId);

        // Build generation configuration
        var config = EntityGenerationConfig.Create(
            gameSystemId: campaign.GameSystemId,
            templateId: template.Id,
            entityTypeName: normalizedEntityType,
            userPrompt: request.UserPrompt,
            fieldsToGenerate: request.FieldsToGenerate,
            existingValues: request.ExistingValues,
            temperature: request.Temperature,
            includeImageGeneration: request.IncludeImageGeneration,
            imageStyle: request.ImageStyle);

        // Create GenerationRequest for traceability
        var inputParameters = BuildInputParameters(config, template.Id);
        var generationRequest = GenerationRequest.Create(
            userId: userId,
            requestType: GenerationRequestType.EntityFieldGeneration,
            targetEntityType: normalizedEntityType,
            campaignId: request.CampaignId,
            inputPrompt: request.UserPrompt,
            inputParameters: inputParameters);

        generationRequest.StartProcessing();

        try
        {
            // Generate entity fields using RAG
            var generationResult = await _entityGenerationService.GenerateEntityFieldsAsync(
                config,
                template,
                campaign.OwnerId, // Use campaign owner for RAG scope
                cancellationToken);

            if (!generationResult.Success)
            {
                // Mark request as failed and persist
                generationRequest.Fail(generationResult.ErrorMessage ?? "Generation failed");
                await PersistGenerationRequestAsync(generationRequest, cancellationToken);

                _logger.LogWarning(
                    "Entity field generation failed for type '{EntityType}': {Error}",
                    normalizedEntityType, generationResult.ErrorMessage);
                
                return GenerateEntityFieldsResult.Failed(
                    generationResult.ErrorMessage ?? "Generation failed",
                    template.Id,
                    normalizedEntityType);
            }

            // Build context sources for transparency
            var contextSources = generationResult.ContextChunks
                .Select((chunk, index) => new RagSourceInfo(
                    $"Context {index + 1}",
                    Guid.Empty, // We don't have doc IDs in the result
                    1.0f))
                .ToList();

            // Handle optional image generation
            string? imageDataUrl = null;
            string? imageUrl = null;
            
            if (request.IncludeImageGeneration)
            {
                _logger.LogInformation(
                    "Image generation requested for entity type '{EntityType}'",
                    normalizedEntityType);
                
                // Create a temporary entity for image generation
                // The actual entity hasn't been created yet, so we create a representation
                var tempEntity = CreateTemporaryEntityForImageGeneration(
                    generationResult,
                    normalizedEntityType,
                    request.CampaignId,
                    userId);
                
                var imageResult = await _entityGenerationService.GenerateEntityImageAsync(
                    tempEntity,
                    template,
                    request.ImageStyle,
                    cancellationToken);

                if (imageResult.Success)
                {
                    imageDataUrl = imageResult.ImageDataUrl;
                    imageUrl = imageResult.StoredImageUrl;
                }
                else
                {
                    // Image generation failure is non-blocking
                    _logger.LogWarning(
                        "Image generation failed for entity type '{EntityType}': {Error}. Continuing without image.",
                        normalizedEntityType, imageResult.ErrorMessage);
                }
            }

            // Mark request as completed and create result record
            generationRequest.Complete();
            
            // Create GenerationResult with all the generation data
            var resultRecord = CreateGenerationResult(
                generationRequest.Id,
                generationResult,
                config.Temperature,
                imageDataUrl,
                imageUrl);

            // Persist GenerationRequest and GenerationResult
            await PersistGenerationRequestWithResultAsync(generationRequest, resultRecord, cancellationToken);

            _logger.LogInformation(
                "Entity field generation completed successfully for type '{EntityType}' " +
                "with {FieldCount} fields generated. GenerationRequestId: {GenerationRequestId}",
                normalizedEntityType, generationResult.GeneratedFields.Count, generationRequest.Id);

            return GenerateEntityFieldsResult.Successful(
                templateId: template.Id,
                entityTypeName: normalizedEntityType,
                generatedFields: new Dictionary<string, object?>(generationResult.GeneratedFields),
                suggestedName: generationResult.SuggestedName,
                suggestedDescription: generationResult.SuggestedDescription,
                imageDataUrl: imageDataUrl,
                imageUrl: imageUrl,
                contextSources: contextSources,
                generationRequestId: generationRequest.Id);
        }
        catch (Exception ex)
        {
            // Mark request as failed and persist
            generationRequest.Fail($"Unexpected error: {ex.Message}");
            await PersistGenerationRequestAsync(generationRequest, cancellationToken);

            _logger.LogError(ex,
                "Unexpected error during entity field generation for type '{EntityType}' in campaign {CampaignId}",
                normalizedEntityType, request.CampaignId);
            
            return GenerateEntityFieldsResult.Failed(
                "An unexpected error occurred during generation. Please try again.",
                template.Id,
                normalizedEntityType);
        }
    }

    /// <summary>
    /// Builds input parameters JSON document for the generation request.
    /// </summary>
    private static JsonDocument BuildInputParameters(EntityGenerationConfig config, Guid templateId)
    {
        var parameters = new
        {
            templateId,
            gameSystemId = config.GameSystemId,
            entityTypeName = config.EntityTypeName,
            temperature = config.Temperature,
            includeImageGeneration = config.IncludeImageGeneration,
            imageStyle = config.ImageStyle,
            fieldsToGenerate = config.FieldsToGenerate.ToList(),
            existingValuesCount = config.ExistingValues.Count
        };

        var json = JsonSerializer.Serialize(parameters);
        return JsonDocument.Parse(json);
    }

    /// <summary>
    /// Creates a GenerationResult record from the generation output.
    /// </summary>
    private static GenerationResult CreateGenerationResult(
        Guid generationRequestId,
        EntityGenerationResult result,
        float temperature,
        string? imageDataUrl,
        string? imageUrl)
    {
        // Build structured output with all generated data
        var structuredOutput = new
        {
            generatedFields = result.GeneratedFields,
            suggestedName = result.SuggestedName,
            suggestedDescription = result.SuggestedDescription,
            contextChunksCount = result.ContextChunks.Count,
            imageDataUrl,
            imageUrl
        };

        var structuredJson = JsonDocument.Parse(JsonSerializer.Serialize(structuredOutput));

        // Build model parameters
        var modelParams = new
        {
            temperature,
            model = DefaultModelName
        };
        var modelParamsJson = JsonDocument.Parse(JsonSerializer.Serialize(modelParams));

        // Build token usage if available
        JsonDocument? tokenUsageJson = null;
        if (result.TokenUsage != null)
        {
            var tokenUsage = new
            {
                promptTokens = result.TokenUsage.PromptTokens,
                completionTokens = result.TokenUsage.CompletionTokens,
                totalTokens = result.TokenUsage.TotalTokens
            };
            tokenUsageJson = JsonDocument.Parse(JsonSerializer.Serialize(tokenUsage));
        }

        return GenerationResult.Create(
            generationRequestId: generationRequestId,
            resultType: "entity_fields",
            sequenceOrder: 1,
            rawOutput: null, // Raw output is large and not typically needed
            structuredOutput: structuredJson,
            modelName: DefaultModelName,
            modelParameters: modelParamsJson,
            tokenUsage: tokenUsageJson);
    }

    /// <summary>
    /// Persists the GenerationRequest to the database.
    /// </summary>
    private async Task PersistGenerationRequestAsync(
        GenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _generationRequestRepository.AddAsync(request, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist GenerationRequest {RequestId}", request.Id);
            // Don't throw - generation tracking failure shouldn't fail the main operation
        }
    }

    /// <summary>
    /// Persists the GenerationRequest and GenerationResult to the database.
    /// </summary>
    private async Task PersistGenerationRequestWithResultAsync(
        GenerationRequest request,
        GenerationResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            await _generationRequestRepository.AddAsync(request, cancellationToken);
            // Add GenerationResult directly to DbContext
            await _dbContext.GenerationResults.AddAsync(result, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogDebug(
                "Persisted GenerationRequest {RequestId} with GenerationResult {ResultId}",
                request.Id, result.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to persist GenerationRequest {RequestId} with result", 
                request.Id);
            // Don't throw - generation tracking failure shouldn't fail the main operation
        }
    }

    /// <summary>
    /// Creates a temporary entity representation for image generation.
    /// </summary>
    private static LoreEntity CreateTemporaryEntityForImageGeneration(
        EntityGenerationResult result,
        string entityType,
        Guid campaignId,
        Guid ownerId)
    {
        return LoreEntity.Create(
            campaignId: campaignId,
            ownerId: ownerId,
            entityType: entityType,
            name: result.SuggestedName ?? "Generated Entity",
            description: result.SuggestedDescription,
            ownershipType: OwnershipType.Master,
            visibility: VisibilityLevel.Draft);
    }
}
