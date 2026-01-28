using MediatR;

namespace Loremaster.Domain.Common;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}
