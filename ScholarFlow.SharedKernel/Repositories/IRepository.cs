namespace ScholarFlow.SharedKernel.Repositories;

/// <summary>
/// Write-side repository backed by EF Core.
/// For complex read queries use Dapper directly via IDbConnection.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Delete(T entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}
