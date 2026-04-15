using ScholarFlow.SharedKernel.Primitives;

namespace ScholarFlow.Domain.Events;

public sealed record TeacherApprovedDomainEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid TeacherProfileId,
    Guid UserId
) : IDomainEvent;
