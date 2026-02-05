using System.Text.Json;
using Loremaster.Domain.Common;

namespace Loremaster.Domain.Entities;

/// <summary>
/// A campaign/game managed by a Master
/// </summary>
public class Campaign : SoftDeletableEntity
{
    public Guid OwnerId { get; private set; }
    public User Owner { get; private set; } = null!;

    public Guid GameSystemId { get; private set; }
    public GameSystem GameSystem { get; private set; } = null!;

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string JoinCode { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Game-system-specific settings (house rules, era, etc.)
    /// Stored as JSONB - structure varies by game system
    /// </summary>
    public JsonDocument? Settings { get; private set; }

    // Navigation properties
    private readonly List<CampaignMember> _members = new();
    public IReadOnlyCollection<CampaignMember> Members => _members.AsReadOnly();

    private readonly List<LoreEntity> _loreEntities = new();
    public IReadOnlyCollection<LoreEntity> LoreEntities => _loreEntities.AsReadOnly();

    private readonly List<GenerationRequest> _generationRequests = new();
    public IReadOnlyCollection<GenerationRequest> GenerationRequests => _generationRequests.AsReadOnly();

    private Campaign() { } // EF Core

    public static Campaign Create(
        Guid ownerId,
        Guid gameSystemId,
        string name,
        string? description = null,
        JsonDocument? settings = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        return new Campaign
        {
            OwnerId = ownerId,
            GameSystemId = gameSystemId,
            Name = name.Trim(),
            Description = description?.Trim(),
            JoinCode = GenerateJoinCode(),
            Settings = settings
        };
    }

    private static string GenerateJoinCode()
    {
        return Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    }

    public void Update(string name, string? description = null, JsonDocument? settings = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();

        if (settings != null)
        {
            Settings?.Dispose();
            Settings = settings;
        }
    }

    public void RegenerateJoinCode()
    {
        JoinCode = GenerateJoinCode();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Transfers campaign ownership to a new user.
    /// </summary>
    /// <param name="newOwnerId">The new owner's user ID.</param>
    public void TransferOwnership(Guid newOwnerId)
    {
        if (newOwnerId == Guid.Empty)
            throw new ArgumentException("New owner ID cannot be empty", nameof(newOwnerId));

        OwnerId = newOwnerId;
    }
}
