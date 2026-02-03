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
/// </summary>
public class GenerateEntityFieldsCommandHandler 
    : IRequestHandler<GenerateEntityFieldsCommand, GenerateEntityFieldsResult>
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly IEntityTemplateRepository _entityTemplateRepository;
    private readonly IEntityGenerationService _entityGenerationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GenerateEntityFieldsCommandHandler> _logger;

    public GenerateEntityFieldsCommandHandler(
        ICampaignRepository campaignRepository,
        ICampaignMemberRepository campaignMemberRepository,
        IEntityTemplateRepository entityTemplateRepository,
        IEntityGenerationService entityGenerationService,
        ICurrentUserService currentUserService,
        ILogger<GenerateEntityFieldsCommandHandler> logger)
    {
        _campaignRepository = campaignRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _entityTemplateRepository = entityTemplateRepository;
        _entityGenerationService = entityGenerationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the generate entity fields command.
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
        
        // Templates are owned by the campaign's Master (OwnerId)
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

            _logger.LogInformation(
                "Entity field generation completed successfully for type '{EntityType}' " +
                "with {FieldCount} fields generated",
                normalizedEntityType, generationResult.GeneratedFields.Count);

            return GenerateEntityFieldsResult.Successful(
                templateId: template.Id,
                entityTypeName: normalizedEntityType,
                generatedFields: new Dictionary<string, object?>(generationResult.GeneratedFields),
                suggestedName: generationResult.SuggestedName,
                suggestedDescription: generationResult.SuggestedDescription,
                imageDataUrl: imageDataUrl,
                imageUrl: imageUrl,
                contextSources: contextSources);
        }
        catch (Exception ex)
        {
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
