using Loremaster.Application.Features.LoreEntities.DTOs;
using Loremaster.Domain.Enums;
using MediatR;

namespace Loremaster.Application.Features.LoreEntities.Commands.UpdateLoreEntity;

/// <summary>
/// Command to update an existing lore entity.
/// </summary>
/// <param name="CampaignId">The campaign the entity belongs to.</param>
/// <param name="EntityId">The ID of the entity to update.</param>
/// <param name="Name">The updated entity name.</param>
/// <param name="Description">Optional updated description.</param>
/// <param name="Visibility">Optional updated visibility level.</param>
/// <param name="ImageUrl">Optional updated image URL.</param>
/// <param name="Attributes">Updated game-system-specific attributes.</param>
/// <param name="Metadata">Updated additional metadata.</param>
public record UpdateLoreEntityCommand(
    Guid CampaignId,
    Guid EntityId,
    string Name,
    string? Description = null,
    VisibilityLevel? Visibility = null,
    string? ImageUrl = null,
    Dictionary<string, object>? Attributes = null,
    Dictionary<string, object>? Metadata = null
) : IRequest<LoreEntityDto>;
