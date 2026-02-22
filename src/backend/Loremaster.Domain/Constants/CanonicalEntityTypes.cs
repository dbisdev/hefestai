namespace Loremaster.Domain.Constants;

/// <summary>
/// Canonical entity type names for template-based entities.
/// These are the standardized type names used across the system.
/// </summary>
public static class CanonicalEntityTypes
{
    public const string Character = "character";
    public const string Actor = "actor";
    public const string Vehicle = "vehicle";
    public const string Monster = "monster";
    
    /// <summary>
    /// All valid canonical entity types for templates.
    /// </summary>
    public static readonly string[] All = 
    {
        Character,
        Actor,
        Vehicle,
        Monster
    };
    
    /// <summary>
    /// Display names for each canonical type (for UI).
    /// </summary>
    public static readonly Dictionary<string, string> DisplayNames = new()
    {
        [Character] = "Personaje",
        [Actor] = "Actor/NPC",
        [Vehicle] = "Vehículo",
        [Monster] = "Monstruo"
    };
    
    /// <summary>
    /// Icons for each canonical type (Material Icons).
    /// </summary>
    public static readonly Dictionary<string, string> Icons = new()
    {
        [Character] = "face",
        [Actor] = "groups",
        [Vehicle] = "rocket_launch",
        [Monster] = "dangerous"
    };
    
    /// <summary>
    /// Checks if a type name is a valid canonical entity type.
    /// </summary>
    public static bool IsValid(string entityTypeName)
    {
        return All.Contains(entityTypeName?.ToLowerInvariant()?.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
