using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Domain.Interfaces.Repositories;

public interface IStreamRepository
{
    Task<List<AcademicStream>> GetAllAsync(CancellationToken ct = default);
    Task<AcademicStream?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task AddAsync(AcademicStream stream, CancellationToken ct = default);
    void Update(AcademicStream stream);
    void Delete(AcademicStream stream);
    Task SaveChangesAsync(CancellationToken ct = default);
}
