using Loremaster.Domain.Common;
using Loremaster.Domain.Enums;

namespace Loremaster.Domain.Entities;

/// <summary>
/// User participation in a campaign
/// </summary>
public class CampaignMember : AuditableEntity
{
    public Guid CampaignId { get; private set; }
    public Campaign Campaign { get; private set; } = null!;

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public CampaignRole Role { get; private set; } = CampaignRole.Player;
    public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;

    // Navigation properties
    private readonly List<GenerationRequest> _generationRequests = new();
    public IReadOnlyCollection<GenerationRequest> GenerationRequests => _generationRequests.AsReadOnly();

    private CampaignMember() { } // EF Core

    public static CampaignMember Create(
        Guid campaignId,
        Guid userId,
        CampaignRole role = CampaignRole.Player)
    {
        return new CampaignMember
        {
            CampaignId = campaignId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
    }

    public void ChangeRole(CampaignRole newRole)
    {
        Role = newRole;
    }

    public bool IsMaster => Role == CampaignRole.Master;
    public bool IsPlayer => Role == CampaignRole.Player;
}
