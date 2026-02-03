using Loremaster.Application.Features.LoreEntities.DTOs;
using MediatR;

namespace Loremaster.Application.Features.LoreEntities.Queries.GetLoreEntityById;

/// <summary>
/// Query to get a single lore entity by its ID.
/// </summary>
/// <param name="CampaignId">The campaign the entity belongs to.</param>
/// <param name="EntityId">The ID of the entity to retrieve.</param>
public record GetLoreEntityByIdQuery(
    Guid CampaignId,
    Guid EntityId
) : IRequest<LoreEntityDto>;
