using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Domain.Interfaces.Repositories;

public interface ITeacherProfileRepository
{
    Task<TeacherProfile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TeacherProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(TeacherProfile profile, CancellationToken ct = default);
    void Update(TeacherProfile profile);
    Task SaveChangesAsync(CancellationToken ct = default);
}
