using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.EntityGeneration.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityGeneration.Commands.GenerateEntityImage;

/// <summary>
/// Handler for GenerateEntityImageCommand.
/// Generates or regenerates an image for an existing entity using AI.
/// </summary>
public class GenerateEntityImageCommandHandler 
    : IRequestHandler<GenerateEntityImageCommand, GenerateEntityImageResult>
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ILoreEntityRepository _loreEntityRepository;
    private readonly IEntityTemplateRepository _entityTemplateRepository;
    private readonly IEntityGenerationService _entityGenerationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GenerateEntityImageCommandHandler> _logger;

    public GenerateEntityImageCommandHandler(
        ICampaignRepository campaignRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ILoreEntityRepository loreEntityRepository,
        IEntityTemplateRepository entityTemplateRepository,
        IEntityGenerationService entityGenerationService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<GenerateEntityImageCommandHandler> logger)
    {
        _campaignRepository = campaignRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _loreEntityRepository = loreEntityRepository;
        _entityTemplateRepository = entityTemplateRepository;
        _entityGenerationService = entityGenerationService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the generate entity image command.
    /// </summary>
    public async Task<GenerateEntityImageResult> Handle(
        GenerateEntityImageCommand request, 
        CancellationToken cancellationToken)
    {
        // Validate authentication
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to generate entity images");
        }

        var userId = _currentUserService.UserId.Value;

        // Validate campaign membership
        var membership = await _campaignMemberRepository
            .GetByCampaignAndUserAsync(request.CampaignId, userId, cancellationToken);
        
        if (membership == null)
        {
            throw new NotFoundException("Campaign", request.CampaignId);
        }

        // Get the entity
        var entity = await _loreEntityRepository.GetByIdAsync(request.EntityId, cancellationToken);
        if (entity == null || entity.CampaignId != request.CampaignId)
        {
            throw new NotFoundException("Entity", request.EntityId);
        }

        // Check write permission on entity
        var isCampaignMaster = membership.Role == Domain.Enums.CampaignRole.Master;
        if (!entity.CanBeWrittenBy(userId, isCampaignMaster))
        {
            throw new ForbiddenAccessException("You don't have permission to modify this entity");
        }

        // Get campaign for game system context
        var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign == null)
        {
            throw new NotFoundException("Campaign", request.CampaignId);
        }

        // Find confirmed template for this entity type
        var template = await _entityTemplateRepository.GetConfirmedTemplateForEntityTypeAsync(
            campaign.GameSystemId,
            campaign.OwnerId,
            entity.EntityType,
            cancellationToken);

        if (template == null)
        {
            _logger.LogWarning(
                "Image generation attempted for entity type '{EntityType}' without confirmed template",
                entity.EntityType);
            
            return GenerateEntityImageResult.Failed(
                $"No confirmed template found for entity type '{entity.EntityType}'. " +
                "A confirmed template is required for image generation.",
                request.EntityId);
        }

        _logger.LogInformation(
            "{Action} image for entity {EntityId} (type: {EntityType}) in campaign {CampaignId} by user {UserId}",
            request.IsRegeneration ? "Regenerating" : "Generating",
            request.EntityId, entity.EntityType, request.CampaignId, userId);

        try
        {
            // Generate the image
            var imageResult = request.IsRegeneration
                ? await _entityGenerationService.RegenerateEntityImageAsync(
                    entity, template, request.Style, cancellationToken)
                : await _entityGenerationService.GenerateEntityImageAsync(
                    entity, template, request.Style, cancellationToken);

            if (!imageResult.Success)
            {
                _logger.LogWarning(
                    "Image generation failed for entity {EntityId}: {Error}",
                    request.EntityId, imageResult.ErrorMessage);
                
                return GenerateEntityImageResult.Failed(
                    imageResult.ErrorMessage ?? "Image generation failed",
                    request.EntityId);
            }

            // Update entity with new image URL if stored
            var bestUrl = imageResult.GetBestUrl();
            if (!string.IsNullOrEmpty(bestUrl))
            {
                entity.SetImageUrl(bestUrl);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation(
                    "Updated entity {EntityId} with generated image URL",
                    request.EntityId);
            }

            _logger.LogInformation(
                "Image generation completed successfully for entity {EntityId}",
                request.EntityId);

            return GenerateEntityImageResult.Successful(
                entityId: request.EntityId,
                imageBase64: imageResult.ImageBase64!,
                storedImageUrl: imageResult.StoredImageUrl,
                generatedPrompt: imageResult.GeneratedPrompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error during image generation for entity {EntityId}",
                request.EntityId);
            
            return GenerateEntityImageResult.Failed(
                "An unexpected error occurred during image generation. Please try again.",
                request.EntityId);
        }
    }
}
