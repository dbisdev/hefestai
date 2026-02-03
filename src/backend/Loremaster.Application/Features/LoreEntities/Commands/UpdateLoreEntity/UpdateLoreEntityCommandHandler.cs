using System.Text.Json;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.LoreEntities.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.LoreEntities.Commands.UpdateLoreEntity;

/// <summary>
/// Handler for UpdateLoreEntityCommand. Updates an existing lore entity.
/// Only users with write permissions can update entities.
/// </summary>
public class UpdateLoreEntityCommandHandler : IRequestHandler<UpdateLoreEntityCommand, LoreEntityDto>
{
    private readonly ILoreEntityRepository _loreEntityRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateLoreEntityCommandHandler> _logger;

    public UpdateLoreEntityCommandHandler(
        ILoreEntityRepository loreEntityRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<UpdateLoreEntityCommandHandler> logger)
    {
        _loreEntityRepository = loreEntityRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the update lore entity command.
    /// </summary>
    /// <param name="request">The update entity command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated entity as a DTO.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user lacks write permissions.</exception>
    /// <exception cref="NotFoundException">Thrown when entity or campaign not found.</exception>
    public async Task<LoreEntityDto> Handle(UpdateLoreEntityCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to update entities");
        }

        var userId = _currentUserService.UserId.Value;

        // Check campaign membership
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
            throw new NotFoundException("LoreEntity", request.EntityId);
        }

        // Check write permission
        if (!entity.CanBeWrittenBy(userId, membership.IsMaster))
        {
            throw new ForbiddenAccessException("You don't have permission to edit this entity");
        }

        // Parse attributes and metadata
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

        // Update the entity
        entity.Update(
            name: request.Name,
            description: request.Description,
            visibility: request.Visibility,
            imageUrl: request.ImageUrl,
            attributes: attributes,
            metadata: metadata
        );

        _loreEntityRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("LoreEntity {EntityId} updated by user {UserId}", request.EntityId, userId);

        return LoreEntityDto.FromEntity(entity);
    }
}
