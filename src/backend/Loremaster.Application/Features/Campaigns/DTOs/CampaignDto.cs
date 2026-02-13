using System.Text.Json;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;

namespace Loremaster.Application.Features.Campaigns.DTOs;

/// <summary>
/// Summary of a game system for embedded display.
/// </summary>
public record GameSystemSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;

    /// <summary>
    /// Creates a GameSystemSummaryDto from a GameSystem entity.
    /// </summary>
    /// <param name="gameSystem">The game system entity.</param>
    /// <returns>A GameSystemSummaryDto instance, or null if the input is null.</returns>
    public static GameSystemSummaryDto? FromEntity(GameSystem? gameSystem)
    {
        if (gameSystem == null) return null;
        return new GameSystemSummaryDto
        {
            Id = gameSystem.Id,
            Name = gameSystem.Name
        };
    }
}

/// <summary>
/// Basic campaign information for list views.
/// </summary>
public record CampaignDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public Guid GameSystemId { get; init; }
    /// <summary>
    /// Game system details (when included in response).
    /// </summary>
    public GameSystemSummaryDto? GameSystem { get; init; }
    public bool IsActive { get; init; }
    public CampaignRole? UserRole { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Maps a Campaign entity to a CampaignDto.
    /// </summary>
    /// <param name="campaign">The campaign entity.</param>
    /// <param name="userRole">The current user's role in the campaign (null if not a member).</param>
    /// <returns>A CampaignDto instance.</returns>
    public static CampaignDto FromEntity(Campaign campaign, CampaignRole? userRole = null)
    {
        return new CampaignDto
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            GameSystemId = campaign.GameSystemId,
            GameSystem = GameSystemSummaryDto.FromEntity(campaign.GameSystem),
            IsActive = campaign.IsActive,
            UserRole = userRole,
            CreatedAt = campaign.CreatedAt
        };
    }
}

/// <summary>
/// Detailed campaign information including settings and member count.
/// </summary>
public record CampaignDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public Guid OwnerId { get; init; }
    public Guid GameSystemId { get; init; }
    /// <summary>
    /// Game system details (when included in response).
    /// </summary>
    public GameSystemSummaryDto? GameSystem { get; init; }
    
    /// <summary>
    /// Only visible to campaign Masters.
    /// </summary>
    public string? JoinCode { get; init; }
    
    public bool IsActive { get; init; }
    public Dictionary<string, object>? Settings { get; init; }
    public CampaignRole? UserRole { get; init; }
    public int MemberCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Maps a Campaign entity to a CampaignDetailDto.
    /// </summary>
    /// <param name="campaign">The campaign entity.</param>
    /// <param name="userRole">The current user's role in the campaign.</param>
    /// <param name="memberCount">Total number of members in the campaign.</param>
    /// <returns>A CampaignDetailDto instance.</returns>
    public static CampaignDetailDto FromEntity(Campaign campaign, CampaignRole? userRole, int memberCount)
    {
        return new CampaignDetailDto
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            OwnerId = campaign.OwnerId,
            GameSystemId = campaign.GameSystemId,
            GameSystem = GameSystemSummaryDto.FromEntity(campaign.GameSystem),
            // Only Masters can see the join code
            JoinCode = userRole == CampaignRole.Master ? campaign.JoinCode : null,
            IsActive = campaign.IsActive,
            Settings = DeserializeJsonDocument(campaign.Settings),
            UserRole = userRole,
            MemberCount = memberCount,
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt
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

/// <summary>
/// Campaign member information.
/// </summary>
public record CampaignMemberDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = null!;
    public CampaignRole Role { get; init; }
    public DateTime JoinedAt { get; init; }

    /// <summary>
    /// Maps a CampaignMember entity to a CampaignMemberDto.
    /// </summary>
    /// <param name="member">The campaign member entity.</param>
    /// <returns>A CampaignMemberDto instance.</returns>
    public static CampaignMemberDto FromEntity(CampaignMember member)
    {
        return new CampaignMemberDto
        {
            Id = member.Id,
            UserId = member.UserId,
            DisplayName = member.User?.DisplayName ?? member.User?.Email ?? "Unknown",
            Role = member.Role,
            JoinedAt = member.JoinedAt
        };
    }
}

/// <summary>
/// Response containing a join code.
/// </summary>
public record JoinCodeDto
{
    public string JoinCode { get; init; } = null!;

    public static JoinCodeDto FromCampaign(Campaign campaign)
    {
        return new JoinCodeDto { JoinCode = campaign.JoinCode };
    }
}
