namespace Loremaster.Domain.Common;

/// <summary>
/// Root base entity that all domain entities inherit from.
/// <para>
/// <c>CreatedAt</c> and <c>UpdatedAt</c> carry <c>protected set</c> so that only EF Core
/// (via explicit <see cref="IAuditableEntity"/> interface implementation) and derived
/// entity classes can mutate the timestamps, preventing accidental writes from
/// application or infrastructure code.
/// </para>
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Gets the entity's unique identifier. Set only during construction.</summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>Gets the UTC timestamp when the entity was first persisted.</summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>Gets the UTC timestamp of the last update, or <c>null</c> if never updated.</summary>
    public DateTime? UpdatedAt { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
