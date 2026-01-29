namespace Loremaster.Domain.Enums;

/// <summary>
/// Ownership type for lore entities
/// </summary>
public enum OwnershipType
{
    /// <summary>
    /// Master-created content (NPCs, locations, items, etc.)
    /// Master has full control
    /// </summary>
    Master = 0,
    
    /// <summary>
    /// Player-owned character
    /// Player has full control, master can only view
    /// </summary>
    Player = 1,
    
    /// <summary>
    /// Shared/collaborative content
    /// Campaign members can edit (if visibility allows)
    /// </summary>
    Shared = 2
}
