using ScholarFlow.SharedKernel.Primitives;

namespace ScholarFlow.SharedKernel.IntegrationEvents;

/// <summary>
/// Published after an admin-assigned subscription and its payment are committed.
/// Consumers must treat delivery as best-effort and must not alter subscription state.
/// </summary>
public sealed record StudentSubscriptionActivatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid UserId,
    string PlanName,
    DateTime StartsAt,
    DateTime EndsAt,
    bool IsScheduled)
    : IIntegrationEvent;
