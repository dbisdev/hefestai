using Loremaster.Domain.Common;
using Loremaster.Domain.Enums;

namespace Loremaster.Domain.Entities;

/// <summary>
/// RAG document source for generation
/// </summary>
public class RagSource : AuditableEntity
{
    public Guid GameSystemId { get; private set; }
    public GameSystem GameSystem { get; private set; } = null!;

    public string Name { get; private set; } = null!;
    public RagSourceType SourceType { get; private set; }
    public string? Version { get; private set; }
    public string? ContentHash { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    private readonly List<GenerationResultSource> _usedInResults = new();
    public IReadOnlyCollection<GenerationResultSource> UsedInResults => _usedInResults.AsReadOnly();

    private RagSource() { } // EF Core

    public static RagSource Create(
        Guid gameSystemId,
        string name,
        RagSourceType sourceType,
        string? version = null,
        string? contentHash = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        return new RagSource
        {
            GameSystemId = gameSystemId,
            Name = name.Trim(),
            SourceType = sourceType,
            Version = version?.Trim(),
            ContentHash = contentHash
        };
    }

    public void Update(string name, string? version = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name.Trim();
        Version = version?.Trim();
    }

    public void UpdateContentHash(string contentHash)
    {
        ContentHash = contentHash;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
