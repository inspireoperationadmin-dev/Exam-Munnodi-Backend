using MediatR;

namespace ScholarFlow.SharedKernel.Primitives;

/// <summary>
/// Marker interface for domain events.
/// Raised by AggregateRoot, dispatched within the same bounded context + transaction.
/// </summary>
public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
