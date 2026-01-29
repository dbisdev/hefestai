namespace Loremaster.Domain.Common;

/// <summary>
/// Base entity with soft delete support
/// </summary>
public abstract class SoftDeletableEntity : AuditableEntity
{
    public DateTime? DeletedAt { get; protected set; }

    public bool IsDeleted => DeletedAt.HasValue;

    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        DeletedAt = null;
    }
}
