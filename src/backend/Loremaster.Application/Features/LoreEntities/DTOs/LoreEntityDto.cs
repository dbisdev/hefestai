using System.Text.Json;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;

namespace Loremaster.Application.Features.LoreEntities.DTOs;

/// <summary>
/// Basic lore entity information for list views.
/// </summary>
public record LoreEntityDto
{
    public Guid Id { get; init; }
    public Guid CampaignId { get; init; }
    public Guid OwnerId { get; init; }
    public string EntityType { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public OwnershipType OwnershipType { get; init; }
    public VisibilityLevel Visibility { get; init; }
    public bool IsTemplate { get; init; }
    public string? ImageUrl { get; init; }
    public Dictionary<string, object>? Attributes { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Maps a LoreEntity domain entity to a LoreEntityDto.
    /// </summary>
    /// <param name="entity">The lore entity.</param>
    /// <returns>A LoreEntityDto instance.</returns>
    public static LoreEntityDto FromEntity(LoreEntity entity)
    {
        return new LoreEntityDto
        {
            Id = entity.Id,
            CampaignId = entity.CampaignId,
            OwnerId = entity.OwnerId,
            EntityType = entity.EntityType,
            Name = entity.Name,
            Description = entity.Description,
            OwnershipType = entity.OwnershipType,
            Visibility = entity.Visibility,
            IsTemplate = entity.IsTemplate,
            ImageUrl = entity.ImageUrl,
            Attributes = DeserializeJsonDocument(entity.Attributes),
            Metadata = DeserializeJsonDocument(entity.Metadata),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// Deserializes a JsonDocument to a Dictionary.
    /// </summary>
    private static Dictionary<string, object>? DeserializeJsonDocument(JsonDocument? doc)
    {
        if (doc == null) return null;
        return JsonSerializer.Deserialize<Dictionary<string, object>>(doc.RootElement.GetRawText());
    }
}
