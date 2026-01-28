using Loremaster.Domain.Common;
using Loremaster.Domain.Enums;

namespace Loremaster.Domain.Entities;

public class Project : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public ProjectStatus Status { get; private set; } = ProjectStatus.Active;
    public Guid OwnerId { get; private set; }
    public User Owner { get; private set; } = null!;
    public DateTime? ArchivedAt { get; private set; }

    private Project() { } // EF Core

    public static Project Create(
        string name,
        Guid ownerId,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name cannot be empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Project name cannot exceed 200 characters", nameof(name));

        return new Project
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            OwnerId = ownerId,
            Status = ProjectStatus.Active
        };
    }

    public void UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name cannot be empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Project name cannot exceed 200 characters", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
    }

    public void Archive()
    {
        if (Status == ProjectStatus.Archived)
            return;

        Status = ProjectStatus.Archived;
        ArchivedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (Status != ProjectStatus.Archived)
            return;

        Status = ProjectStatus.Active;
        ArchivedAt = null;
    }

    public bool IsOwnedBy(Guid userId) => OwnerId == userId;
}
