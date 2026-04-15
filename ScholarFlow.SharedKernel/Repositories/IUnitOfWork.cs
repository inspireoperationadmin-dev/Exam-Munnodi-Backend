namespace ScholarFlow.SharedKernel.Repositories;

/// <summary>
/// Unit of Work — wraps EF SaveChanges and transaction management.
/// Use only on the Command (write) side.
/// Domain events are dispatched automatically inside ApplicationDbContext.SaveChangesAsync.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
