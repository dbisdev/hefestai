using Loremaster.Application.Features.LoreEntities.DTOs;
using Loremaster.Domain.Enums;
using MediatR;

namespace Loremaster.Application.Features.LoreEntities.Commands.CreateLoreEntity;

/// <summary>
/// Command to create a new lore entity within a campaign.
/// </summary>
/// <param name="CampaignId">The campaign this entity belongs to.</param>
/// <param name="EntityType">The type of entity (e.g., character, location, item).</param>
/// <param name="Name">The entity name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="OwnershipType">Who controls the entity (defaults to Master or Player based on role).</param>
/// <param name="Visibility">Who can see the entity.</param>
/// <param name="IsTemplate">Whether this is a template entity.</param>
/// <param name="ImageUrl">Optional image URL.</param>
/// <param name="Attributes">Game-system-specific attributes.</param>
/// <param name="Metadata">Additional flexible metadata.</param>
public record CreateLoreEntityCommand(
    Guid CampaignId,
    string EntityType,
    string Name,
    string? Description = null,
    OwnershipType? OwnershipType = null,
    VisibilityLevel? Visibility = null,
    bool? IsTemplate = null,
    string? ImageUrl = null,
    Dictionary<string, object>? Attributes = null,
    Dictionary<string, object>? Metadata = null
) : IRequest<LoreEntityDto>;
