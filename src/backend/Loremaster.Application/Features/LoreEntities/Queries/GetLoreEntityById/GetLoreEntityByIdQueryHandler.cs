using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.LoreEntities.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.LoreEntities.Queries.GetLoreEntityById;

/// <summary>
/// Handler for GetLoreEntityByIdQuery. Returns a single entity by ID.
/// Only returns the entity if the user has read permissions.
/// </summary>
public class GetLoreEntityByIdQueryHandler : IRequestHandler<GetLoreEntityByIdQuery, LoreEntityDto>
{
    private readonly ILoreEntityRepository _loreEntityRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetLoreEntityByIdQueryHandler(
        ILoreEntityRepository loreEntityRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService)
    {
        _loreEntityRepository = loreEntityRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Handles the get lore entity by ID query.
    /// </summary>
    /// <param name="request">The query with campaign and entity IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity as a DTO.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user lacks read permissions.</exception>
    /// <exception cref="NotFoundException">Thrown when entity or campaign not found.</exception>
    public async Task<LoreEntityDto> Handle(GetLoreEntityByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to view entities");
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

        // Check read permission
        if (!entity.CanBeReadBy(userId, true, membership.IsMaster))
        {
            throw new ForbiddenAccessException("You don't have permission to view this entity");
        }

        return LoreEntityDto.FromEntity(entity);
    }
}
