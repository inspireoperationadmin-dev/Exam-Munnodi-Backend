namespace ScholarFlow.SharedKernel.Primitives;

/// <summary>
/// Implemented by AggregateRoot classes that can raise domain events.
/// ApplicationDbContext uses this interface to dispatch events after SaveChanges.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
