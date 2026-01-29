using Loremaster.Domain.Common;

namespace Loremaster.Domain.Entities;

/// <summary>
/// Supported tabletop RPG game systems
/// </summary>
public class GameSystem : AuditableEntity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Publisher { get; private set; }
    public string? Version { get; private set; }
    public string? Description { get; private set; }
    public List<string> SupportedEntityTypes { get; private set; } = new();
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    private readonly List<Campaign> _campaigns = new();
    public IReadOnlyCollection<Campaign> Campaigns => _campaigns.AsReadOnly();

    private readonly List<RagSource> _ragSources = new();
    public IReadOnlyCollection<RagSource> RagSources => _ragSources.AsReadOnly();

    private GameSystem() { } // EF Core

    public static GameSystem Create(
        string code,
        string name,
        string? publisher = null,
        string? version = null,
        string? description = null,
        List<string>? supportedEntityTypes = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        return new GameSystem
        {
            Code = code.ToLowerInvariant().Trim(),
            Name = name.Trim(),
            Publisher = publisher?.Trim(),
            Version = version?.Trim(),
            Description = description?.Trim(),
            SupportedEntityTypes = supportedEntityTypes ?? new List<string>()
        };
    }

    public void Update(
        string name,
        string? publisher = null,
        string? version = null,
        string? description = null,
        List<string>? supportedEntityTypes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name.Trim();
        Publisher = publisher?.Trim();
        Version = version?.Trim();
        Description = description?.Trim();

        if (supportedEntityTypes != null)
            SupportedEntityTypes = supportedEntityTypes;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
