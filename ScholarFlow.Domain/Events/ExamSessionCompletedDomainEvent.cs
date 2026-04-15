using ScholarFlow.Domain.ValueObjects;
using ScholarFlow.SharedKernel.Primitives;

namespace ScholarFlow.Domain.Events;

public sealed record ExamSessionCompletedDomainEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid SessionId,
    Guid UserId,
    Guid? SubjectId,
    Guid? PaperId,
    ExamScore Score,
    bool IsPractice
) : IDomainEvent;
