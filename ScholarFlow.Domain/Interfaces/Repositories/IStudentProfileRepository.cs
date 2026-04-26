using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Domain.Interfaces.Repositories;

public interface IStudentProfileRepository
{
    Task<StudentProfile?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Includes AcademicStream and SubjectSelections.Subject for profile queries.</summary>
    Task<StudentProfile?> GetByUserIdWithDetailsAsync(Guid userId, CancellationToken ct = default);

    Task<StudentProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool>            ExistsByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task                  AddAsync(StudentProfile profile, CancellationToken ct = default);
    void                  Update(StudentProfile profile);

    // ── Subject selections ────────────────────────────────────────────────────
    Task AddSubjectSelectionAsync(StudentSubjectSelection selection, CancellationToken ct = default);
    void RemoveSubjectSelections(IEnumerable<StudentSubjectSelection> selections);

    Task SaveChangesAsync(CancellationToken ct = default);
}