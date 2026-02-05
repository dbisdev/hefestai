using Loremaster.Domain.Enums;

namespace Loremaster.Application.Features.Admin.DTOs;

/// <summary>
/// DTO representing a user for admin management purposes.
/// Contains full user information visible to administrators.
/// </summary>
public record AdminUserDto
{
    /// <summary>User unique identifier.</summary>
    public Guid Id { get; init; }
    
    /// <summary>User email address.</summary>
    public string Email { get; init; } = null!;
    
    /// <summary>User display name.</summary>
    public string? DisplayName { get; init; }
    
    /// <summary>User role (Player, Master, Admin).</summary>
    public UserRole Role { get; init; }
    
    /// <summary>User avatar URL.</summary>
    public string? AvatarUrl { get; init; }
    
    /// <summary>Whether the user account is active.</summary>
    public bool IsActive { get; init; }
    
    /// <summary>Last login timestamp.</summary>
    public DateTime? LastLoginAt { get; init; }
    
    /// <summary>Account creation timestamp.</summary>
    public DateTime CreatedAt { get; init; }
    
    /// <summary>Last update timestamp.</summary>
    public DateTime? UpdatedAt { get; init; }
    
    /// <summary>Number of campaigns owned by this user.</summary>
    public int OwnedCampaignsCount { get; init; }
    
    /// <summary>Number of campaign memberships.</summary>
    public int CampaignMembershipsCount { get; init; }
}
