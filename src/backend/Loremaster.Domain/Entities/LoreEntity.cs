using System.Text.Json;
using Loremaster.Domain.Common;
using Loremaster.Domain.Enums;

namespace Loremaster.Domain.Entities;

/// <summary>
/// Polymorphic base entity for all lore content (characters, NPCs, vehicles, etc.)
/// </summary>
public class LoreEntity : SoftDeletableEntity
{
    public Guid CampaignId { get; private set; }
    public Campaign Campaign { get; private set; } = null!;

    public Guid OwnerId { get; private set; }
    public User Owner { get; private set; } = null!;

    public Guid? GenerationRequestId { get; private set; }
    public GenerationRequest? GenerationRequest { get; private set; }

    public string EntityType { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    
    /// <summary>
    /// Who controls this entity: master (GM content), player (PC), shared (collaborative)
    /// </summary>
    public OwnershipType OwnershipType { get; private set; } = OwnershipType.Master;
    
    /// <summary>
    /// Who can see: draft (owner only), private (owner+master), campaign (members), public (all)
    /// </summary>
    public VisibilityLevel Visibility { get; private set; } = VisibilityLevel.Campaign;
    
    public bool IsTemplate { get; private set; }
    public string? ImageUrl { get; private set; }

    /// <summary>
    /// Game-system-specific mechanical attributes
    /// Stored as JSONB - structure varies by game system AND entity type
    /// </summary>
    public JsonDocument? Attributes { get; private set; }

    /// <summary>
    /// Non-mechanical flexible data (tags, notes, custom fields)
    /// Stored as JSONB - user-defined structure
    /// </summary>
    public JsonDocument? Metadata { get; private set; }

    // Navigation properties
    private readonly List<LoreEntityRelationship> _outgoingRelationships = new();
    public IReadOnlyCollection<LoreEntityRelationship> OutgoingRelationships => _outgoingRelationships.AsReadOnly();

    private readonly List<LoreEntityRelationship> _incomingRelationships = new();
    public IReadOnlyCollection<LoreEntityRelationship> IncomingRelationships => _incomingRelationships.AsReadOnly();

    private readonly List<LoreEntityImport> _imports = new();
    public IReadOnlyCollection<LoreEntityImport> Imports => _imports.AsReadOnly();

    private LoreEntity() { } // EF Core

    public static LoreEntity Create(
        Guid campaignId,
        Guid ownerId,
        string entityType,
        string name,
        string? description = null,
        OwnershipType ownershipType = OwnershipType.Master,
        VisibilityLevel visibility = VisibilityLevel.Campaign,
        bool isTemplate = false,
        string? imageUrl = null,
        JsonDocument? attributes = null,
        JsonDocument? metadata = null,
        Guid? generationRequestId = null)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type cannot be empty", nameof(entityType));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        return new LoreEntity
        {
            CampaignId = campaignId,
            OwnerId = ownerId,
            EntityType = entityType.ToLowerInvariant().Trim(),
            Name = name.Trim(),
            Description = description?.Trim(),
            OwnershipType = ownershipType,
            Visibility = visibility,
            IsTemplate = isTemplate,
            ImageUrl = imageUrl?.Trim(),
            Attributes = attributes,
            Metadata = metadata,
            GenerationRequestId = generationRequestId
        };
    }

    /// <summary>
    /// Create a player-owned character
    /// </summary>
    public static LoreEntity CreatePlayerCharacter(
        Guid campaignId,
        Guid playerId,
        string name,
        string? description = null,
        VisibilityLevel visibility = VisibilityLevel.Draft,
        JsonDocument? attributes = null,
        JsonDocument? metadata = null)
    {
        return Create(
            campaignId: campaignId,
            ownerId: playerId,
            entityType: "character",
            name: name,
            description: description,
            ownershipType: OwnershipType.Player,
            visibility: visibility,
            attributes: attributes,
            metadata: metadata);
    }

    public void Update(
        string name,
        string? description = null,
        VisibilityLevel? visibility = null,
        string? imageUrl = null,
        JsonDocument? attributes = null,
        JsonDocument? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();

        if (visibility.HasValue)
            Visibility = visibility.Value;

        ImageUrl = imageUrl?.Trim();

        if (attributes != null)
        {
            Attributes?.Dispose();
            Attributes = attributes;
        }

        if (metadata != null)
        {
            Metadata?.Dispose();
            Metadata = metadata;
        }
    }

    public void SetAsTemplate(bool isTemplate = true)
    {
        IsTemplate = isTemplate;
    }

    public void ChangeVisibility(VisibilityLevel visibility)
    {
        Visibility = visibility;
    }

    public void ChangeOwnershipType(OwnershipType ownershipType)
    {
        OwnershipType = ownershipType;
    }

    public void TransferOwnership(Guid newOwnerId, OwnershipType? newOwnershipType = null)
    {
        OwnerId = newOwnerId;
        if (newOwnershipType.HasValue)
            OwnershipType = newOwnershipType.Value;
    }

    public void LinkToGenerationRequest(Guid generationRequestId)
    {
        GenerationRequestId = generationRequestId;
    }

    /// <summary>
    /// Check if a user can read this entity
    /// </summary>
    public bool CanBeReadBy(Guid userId, bool isCampaignMember, bool isCampaignMaster)
    {
        // Owner can always read
        if (OwnerId == userId) return true;
        
        // Public visibility - anyone can read
        if (Visibility == VisibilityLevel.Public) return true;
        
        // Campaign visibility - members can read
        if (Visibility == VisibilityLevel.Campaign && isCampaignMember) return true;
        
        // Private visibility - only master can read (besides owner)
        if (Visibility == VisibilityLevel.Private && isCampaignMaster) return true;
        
        // Draft - only owner (already handled above)
        return false;
    }

    /// <summary>
    /// Check if a user can write/edit this entity
    /// </summary>
    public bool CanBeWrittenBy(Guid userId, bool isCampaignMaster)
    {
        // Owner can always write
        if (OwnerId == userId) return true;
        
        // Player-owned entities can only be edited by the player
        if (OwnershipType == OwnershipType.Player) return false;
        
        // Master and shared entities can be edited by the campaign master
        return isCampaignMaster;
    }

    public bool IsPlayerCharacter => EntityType == "character" && OwnershipType == OwnershipType.Player;
}
