using ScholarFlow.SharedKernel.Primitives;

namespace ScholarFlow.Domain.Entities.Base;

/// <summary>
/// Base for aggregates that raise domain events but do NOT need audit trail.
/// e.g. ExamSession, TeacherProfile
/// </summary>
public abstract class AggregateRoot : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; protected set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
