namespace Loremaster.Domain.Common;

public abstract class BaseDomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
