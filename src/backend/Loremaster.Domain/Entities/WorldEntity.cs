using System.Text.Json;
using Loremaster.Domain.Common;
using Loremaster.Domain.Enums;

namespace Loremaster.Domain.Entities;

public class WorldEntity : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string Type { get; private set; } = null!;
    public string Meta { get; private set; } = null!;
    public string Image { get; private set; } = null!;
    public EntityCategory Category { get; private set; }
    public string? Description { get; private set; }
    public string? StatsJson { get; private set; }
    
    // Creator (Master) who owns this entity
    public Guid CreatorId { get; private set; }
    public User Creator { get; private set; } = null!;

    private WorldEntity() { } // EF Core

    public static WorldEntity Create(
        string name,
        string type,
        string meta,
        string image,
        EntityCategory category,
        Guid creatorId,
        string? description = null,
        Dictionary<string, object>? stats = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be empty", nameof(type));

        return new WorldEntity
        {
            Name = name.Trim(),
            Type = type.Trim(),
            Meta = meta?.Trim() ?? string.Empty,
            Image = image ?? string.Empty,
            Category = category,
            CreatorId = creatorId,
            Description = description?.Trim(),
            StatsJson = stats != null ? JsonSerializer.Serialize(stats) : null
        };
    }

    public void UpdateDetails(
        string name,
        string type,
        string meta,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name.Trim();
        Type = type.Trim();
        Meta = meta?.Trim() ?? string.Empty;
        Description = description?.Trim();
    }

    public void UpdateImage(string image)
    {
        Image = image ?? string.Empty;
    }

    public void UpdateStats(Dictionary<string, object>? stats)
    {
        StatsJson = stats != null ? JsonSerializer.Serialize(stats) : null;
    }

    public Dictionary<string, object>? GetStats()
    {
        if (string.IsNullOrEmpty(StatsJson))
            return null;
            
        return JsonSerializer.Deserialize<Dictionary<string, object>>(StatsJson);
    }

    public bool IsOwnedBy(Guid userId) => CreatorId == userId;
}
