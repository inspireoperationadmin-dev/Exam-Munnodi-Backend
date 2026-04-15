using ScholarFlow.SharedKernel.Primitives;

namespace ScholarFlow.Domain.Events;

public sealed record TeacherRejectedDomainEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid TeacherProfileId,
    Guid UserId,
    string Reason
) : IDomainEvent;
