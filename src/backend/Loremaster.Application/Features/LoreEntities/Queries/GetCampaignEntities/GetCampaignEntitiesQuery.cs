using Loremaster.Application.Common.Models;
using Loremaster.Application.Features.LoreEntities.DTOs;
using Loremaster.Domain.Enums;
using MediatR;

namespace Loremaster.Application.Features.LoreEntities.Queries.GetCampaignEntities;

/// <summary>
/// Query to get visible lore entities in a campaign for the current user with optional pagination.
/// </summary>
/// <param name="CampaignId">The campaign ID to get entities from.</param>
/// <param name="EntityType">Optional filter by entity type.</param>
/// <param name="Visibility">Optional filter by visibility level.</param>
/// <param name="SearchTerm">Optional search term to filter by name or description.</param>
/// <param name="PageNumber">Page number for pagination (1-based). Null for unpaginated results.</param>
/// <param name="PageSize">Number of items per page. Default is 20, max is 100.</param>
public record GetCampaignEntitiesQuery(
    Guid CampaignId,
    string? EntityType = null,
    VisibilityLevel? Visibility = null,
    string? SearchTerm = null,
    int? PageNumber = null,
    int PageSize = 20
) : IRequest<GetCampaignEntitiesResult>;

/// <summary>
/// Result of GetCampaignEntitiesQuery supporting both paginated and unpaginated results.
/// </summary>
/// <param name="Items">The entities for the current page.</param>
/// <param name="TotalCount">Total number of matching entities.</param>
/// <param name="PageNumber">Current page number (null if unpaginated).</param>
/// <param name="TotalPages">Total number of pages (null if unpaginated).</param>
/// <param name="HasNextPage">Whether there are more pages.</param>
/// <param name="HasPreviousPage">Whether there are previous pages.</param>
public record GetCampaignEntitiesResult(
    IReadOnlyCollection<LoreEntityDto> Items,
    int TotalCount,
    int? PageNumber,
    int? TotalPages,
    bool HasNextPage,
    bool HasPreviousPage)
{
    /// <summary>
    /// Creates an unpaginated result with all items.
    /// </summary>
    public static GetCampaignEntitiesResult Unpaginated(IReadOnlyCollection<LoreEntityDto> items)
        => new(items, items.Count, null, null, false, false);

    /// <summary>
    /// Creates a paginated result from a PaginatedList.
    /// </summary>
    public static GetCampaignEntitiesResult FromPaginatedList(PaginatedList<LoreEntityDto> paginatedList)
        => new(
            paginatedList.Items,
            paginatedList.TotalCount,
            paginatedList.PageNumber,
            paginatedList.TotalPages,
            paginatedList.HasNextPage,
            paginatedList.HasPreviousPage);
}
