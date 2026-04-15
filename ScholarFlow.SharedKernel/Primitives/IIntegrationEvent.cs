using MediatR;

namespace ScholarFlow.SharedKernel.Primitives;

/// <summary>
/// Marker interface for integration events.
/// Published AFTER the domain event handler completes — crosses module boundaries.
/// Analytics, Notifications etc. subscribe to these.
/// </summary>
public interface IIntegrationEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
