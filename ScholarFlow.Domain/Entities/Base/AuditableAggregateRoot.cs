using ScholarFlow.Domain.Interfaces;
using ScholarFlow.SharedKernel.Primitives;

namespace ScholarFlow.Domain.Entities.Base;

/// <summary>
/// Base for aggregates that need both audit trail + domain events.
/// e.g. Paper, Question
/// </summary>
public abstract class AuditableAggregateRoot : IAuditable, ISoftDeletable, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; protected set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
