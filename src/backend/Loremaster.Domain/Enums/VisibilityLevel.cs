namespace Loremaster.Domain.Enums;

/// <summary>
/// Visibility level for lore entities
/// </summary>
public enum VisibilityLevel
{
    /// <summary>
    /// Only owner can see (work in progress)
    /// </summary>
    Draft = 0,
    
    /// <summary>
    /// Owner and campaign master can see
    /// </summary>
    Private = 1,
    
    /// <summary>
    /// All campaign members can see
    /// </summary>
    Campaign = 2,
    
    /// <summary>
    /// Anyone can see (public gallery)
    /// </summary>
    Public = 3
}
