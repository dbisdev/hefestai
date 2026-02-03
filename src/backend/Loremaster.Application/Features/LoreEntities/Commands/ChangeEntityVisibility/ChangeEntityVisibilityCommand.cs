using Loremaster.Application.Features.LoreEntities.DTOs;
using Loremaster.Domain.Enums;
using MediatR;

namespace Loremaster.Application.Features.LoreEntities.Commands.ChangeEntityVisibility;

/// <summary>
/// Command to change the visibility level of a lore entity.
/// </summary>
/// <param name="CampaignId">The campaign the entity belongs to.</param>
/// <param name="EntityId">The ID of the entity to update.</param>
/// <param name="Visibility">The new visibility level.</param>
public record ChangeEntityVisibilityCommand(
    Guid CampaignId,
    Guid EntityId,
    VisibilityLevel Visibility
) : IRequest<LoreEntityDto>;
