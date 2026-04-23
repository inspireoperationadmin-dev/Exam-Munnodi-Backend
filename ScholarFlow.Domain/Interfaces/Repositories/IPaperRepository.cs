using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Interfaces.Repositories;

public interface IPaperRepository
{
    Task<List<Paper>> GetAllAsync(Guid? subjectId, PaperType? type, PaperMedium? medium, int? year, CancellationToken ct = default);
    Task<Paper?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByCompositeKeyAsync(string title, Guid? subjectId, int year, PaperType type, PaperMedium medium, CancellationToken ct = default);
    Task<bool> ExistsByCompositeKeyExcludingIdAsync(Guid excludeId, string title, Guid? subjectId, int year, PaperType type, PaperMedium medium, CancellationToken ct = default);
    Task<bool> IsTeacherOwnerAsync(Guid paperId, Guid userId, CancellationToken ct = default);
    Task<Guid?> GetTeacherProfileIdByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Paper paper, CancellationToken ct = default);
    void Update(Paper paper);
    void Delete(Paper paper);
    Task SaveChangesAsync(CancellationToken ct = default);
}