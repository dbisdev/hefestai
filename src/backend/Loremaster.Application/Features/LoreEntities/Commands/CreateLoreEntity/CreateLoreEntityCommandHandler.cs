using System.Text.Json;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.LoreEntities.DTOs;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.LoreEntities.Commands.CreateLoreEntity;

/// <summary>
/// Handler for CreateLoreEntityCommand. Creates a new lore entity within a campaign.
/// Only campaign members can create entities.
/// Entity creation is validated against confirmed templates for the campaign's game system.
/// </summary>
public class CreateLoreEntityCommandHandler : IRequestHandler<CreateLoreEntityCommand, LoreEntityDto>
{
    private readonly ILoreEntityRepository _loreEntityRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IEntityTemplateRepository _entityTemplateRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateLoreEntityCommandHandler> _logger;

    public CreateLoreEntityCommandHandler(
        ILoreEntityRepository loreEntityRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICampaignRepository campaignRepository,
        IEntityTemplateRepository entityTemplateRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<CreateLoreEntityCommandHandler> logger)
    {
        _loreEntityRepository = loreEntityRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _campaignRepository = campaignRepository;
        _entityTemplateRepository = entityTemplateRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the create lore entity command.
    /// </summary>
    /// <param name="request">The create entity command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created entity as a DTO.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user is not authenticated or not a campaign member.</exception>
    /// <exception cref="ValidationException">Thrown when no confirmed template exists for the entity type.</exception>
    public async Task<LoreEntityDto> Handle(CreateLoreEntityCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to create entities");
        }

        var userId = _currentUserService.UserId.Value;

        // Check campaign membership
        var membership = await _campaignMemberRepository
            .GetByCampaignAndUserAsync(request.CampaignId, userId, cancellationToken);
        
        if (membership == null)
        {
            throw new NotFoundException("Campaign", request.CampaignId);
        }

        // Get the campaign to access GameSystemId and OwnerId
        var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign == null)
        {
            throw new NotFoundException("Campaign", request.CampaignId);
        }

        // Validate entity type against confirmed templates
        // Templates are either owned by the campaign's Master (OwnerId) or by an Admin (shared globally)
        var normalizedEntityType = EntityTemplate.NormalizeEntityTypeName(request.EntityType);
        var template = await _entityTemplateRepository.GetConfirmedTemplateForEntityTypeAsync(
            campaign.GameSystemId,
            campaign.OwnerId,
            normalizedEntityType,
            cancellationToken);

        if (template == null)
        {
            _logger.LogWarning(
                "Entity creation rejected: No confirmed template for type '{EntityType}' in game system {GameSystemId}",
                normalizedEntityType, campaign.GameSystemId);
            
            throw new ValidationException(
                $"Entity type '{request.EntityType}' is not available. " +
                $"A confirmed template must exist before entities of this type can be created.");
        }

        // Validate attributes against template if provided
        if (request.Attributes != null && request.Attributes.Count > 0)
        {
            var attributesDict = request.Attributes
                .ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);
            
            var validationResult = template.ValidateEntityAttributes(attributesDict);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Entity attributes validation failed for type '{EntityType}': {Errors}",
                    normalizedEntityType, string.Join("; ", validationResult.Errors));
                
                throw new ValidationException(
                    $"Entity attributes are invalid: {string.Join("; ", validationResult.Errors)}");
            }
        }

        // Determine ownership type based on role and request
        var ownershipType = request.OwnershipType ?? 
            (membership.IsMaster ? OwnershipType.Master : OwnershipType.Player);
        
        // Players can only create player-owned entities
        if (!membership.IsMaster && ownershipType != OwnershipType.Player)
        {
            throw new ForbiddenAccessException("Players can only create player-owned entities");
        }

        // Parse visibility
        var visibility = request.Visibility ?? VisibilityLevel.Campaign;

        // Parse attributes and metadata from request
        JsonDocument? attributes = null;
        JsonDocument? metadata = null;

        if (request.Attributes != null)
        {
            attributes = JsonDocument.Parse(JsonSerializer.Serialize(request.Attributes));
        }

        if (request.Metadata != null)
        {
            metadata = JsonDocument.Parse(JsonSerializer.Serialize(request.Metadata));
        }

        // Create the entity using the normalized entity type from the template
        var entity = LoreEntity.Create(
            campaignId: request.CampaignId,
            ownerId: userId,
            entityType: normalizedEntityType,
            name: request.Name,
            description: request.Description,
            ownershipType: ownershipType,
            visibility: visibility,
            isTemplate: request.IsTemplate ?? false,
            imageUrl: request.ImageUrl,
            attributes: attributes,
            metadata: metadata
        );

        await _loreEntityRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "LoreEntity {EntityId} of type {EntityType} created by user {UserId} in campaign {CampaignId} using template {TemplateId}", 
            entity.Id, entity.EntityType, userId, request.CampaignId, template.Id);

        return LoreEntityDto.FromEntity(entity);
    }
}
