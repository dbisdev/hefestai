using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Common.Models;
using Loremaster.Application.Features.LoreEntities.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.LoreEntities.Queries.GetCampaignEntities;

/// <summary>
/// Handler for GetCampaignEntitiesQuery. Returns visible entities in a campaign with optional pagination.
/// Applies visibility filtering based on user's role and permissions.
/// </summary>
public class GetCampaignEntitiesQueryHandler : IRequestHandler<GetCampaignEntitiesQuery, GetCampaignEntitiesResult>
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    private readonly ILoreEntityRepository _loreEntityRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCampaignEntitiesQueryHandler(
        ILoreEntityRepository loreEntityRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService)
    {
        _loreEntityRepository = loreEntityRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Handles the get campaign entities query with optional pagination.
    /// </summary>
    /// <param name="request">The query with campaign ID, filters, and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated or unpaginated collection of visible entities as DTOs.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user is not authenticated.</exception>
    /// <exception cref="NotFoundException">Thrown when campaign not found or user is not a member.</exception>
    public async Task<GetCampaignEntitiesResult> Handle(
        GetCampaignEntitiesQuery request, 
        CancellationToken cancellationToken)
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

        // Get entities based on type filter
        IEnumerable<Domain.Entities.LoreEntity> entities;
        
        if (!string.IsNullOrEmpty(request.EntityType))
        {
            entities = await _loreEntityRepository
                .GetByCampaignAndTypeAsync(request.CampaignId, request.EntityType, cancellationToken);
        }
        else
        {
            entities = await _loreEntityRepository
                .GetVisibleToCampaignMemberAsync(request.CampaignId, userId, membership.IsMaster, cancellationToken);
        }

        // Apply additional visibility filter if specified
        if (request.Visibility.HasValue)
        {
            entities = entities.Where(e => e.Visibility == request.Visibility.Value);
        }

        // Filter based on read permissions
        var visibleEntities = entities
            .Where(e => e.CanBeReadBy(userId, true, membership.IsMaster));

        // Apply search filter if specified
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTermLower = request.SearchTerm.Trim().ToLowerInvariant();
            visibleEntities = visibleEntities.Where(e =>
                e.Name.ToLowerInvariant().Contains(searchTermLower) ||
                (e.Description != null && e.Description.ToLowerInvariant().Contains(searchTermLower)));
        }

        // Convert to list for pagination
        var entityList = visibleEntities.ToList();

        // If pagination is not requested, return all results
        if (!request.PageNumber.HasValue)
        {
            var allItems = entityList.Select(LoreEntityDto.FromEntity).ToList();
            return GetCampaignEntitiesResult.Unpaginated(allItems);
        }

        // Apply pagination
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var pageNumber = Math.Max(1, request.PageNumber.Value);
        var totalCount = entityList.Count;

        var pagedEntities = entityList
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(LoreEntityDto.FromEntity)
            .ToList();

        var paginatedList = new PaginatedList<LoreEntityDto>(pagedEntities, totalCount, pageNumber, pageSize);
        return GetCampaignEntitiesResult.FromPaginatedList(paginatedList);
    }
}
