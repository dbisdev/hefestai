namespace Loremaster.Application.Features.Admin.DTOs;

/// <summary>
/// DTO representing a campaign for admin management purposes.
/// Contains full campaign information visible to administrators.
/// </summary>
public record AdminCampaignDto
{
    /// <summary>Campaign unique identifier.</summary>
    public Guid Id { get; init; }
    
    /// <summary>Campaign name.</summary>
    public string Name { get; init; } = null!;
    
    /// <summary>Campaign description.</summary>
    public string? Description { get; init; }
    
    /// <summary>Join code for players to join the campaign.</summary>
    public string JoinCode { get; init; } = null!;
    
    /// <summary>Whether the campaign is active.</summary>
    public bool IsActive { get; init; }
    
    /// <summary>Owner/creator user ID.</summary>
    public Guid OwnerId { get; init; }
    
    /// <summary>Owner display name or email.</summary>
    public string OwnerName { get; init; } = null!;
    
    /// <summary>Game system ID.</summary>
    public Guid GameSystemId { get; init; }
    
    /// <summary>Game system name.</summary>
    public string GameSystemName { get; init; } = null!;
    
    /// <summary>Number of members in the campaign.</summary>
    public int MemberCount { get; init; }
    
    /// <summary>Number of lore entities in the campaign.</summary>
    public int EntityCount { get; init; }
    
    /// <summary>Campaign creation timestamp.</summary>
    public DateTime CreatedAt { get; init; }
    
    /// <summary>Last update timestamp.</summary>
    public DateTime? UpdatedAt { get; init; }
}
