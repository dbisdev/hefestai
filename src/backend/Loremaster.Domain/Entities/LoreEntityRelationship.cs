using Loremaster.Domain.Common;

namespace Loremaster.Domain.Entities;

/// <summary>
/// Relationship between two lore entities
/// </summary>
public class LoreEntityRelationship : BaseEntity
{
    public Guid SourceEntityId { get; private set; }
    public LoreEntity SourceEntity { get; private set; } = null!;

    public Guid TargetEntityId { get; private set; }
    public LoreEntity TargetEntity { get; private set; } = null!;

    public string RelationshipType { get; private set; } = null!;
    public string? Description { get; private set; }

    private LoreEntityRelationship() { } // EF Core

    public static LoreEntityRelationship Create(
        Guid sourceEntityId,
        Guid targetEntityId,
        string relationshipType,
        string? description = null)
    {
        if (sourceEntityId == targetEntityId)
            throw new ArgumentException("Cannot create a relationship to self");

        if (string.IsNullOrWhiteSpace(relationshipType))
            throw new ArgumentException("Relationship type cannot be empty", nameof(relationshipType));

        return new LoreEntityRelationship
        {
            SourceEntityId = sourceEntityId,
            TargetEntityId = targetEntityId,
            RelationshipType = relationshipType.ToLowerInvariant().Trim(),
            Description = description?.Trim()
        };
    }

    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
    }
}
