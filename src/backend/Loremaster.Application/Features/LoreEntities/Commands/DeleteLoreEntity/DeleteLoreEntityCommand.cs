using MediatR;

namespace Loremaster.Application.Features.LoreEntities.Commands.DeleteLoreEntity;

/// <summary>
/// Command to soft-delete a lore entity.
/// </summary>
/// <param name="CampaignId">The campaign the entity belongs to.</param>
/// <param name="EntityId">The ID of the entity to delete.</param>
public record DeleteLoreEntityCommand(
    Guid CampaignId,
    Guid EntityId
) : IRequest<Unit>;
