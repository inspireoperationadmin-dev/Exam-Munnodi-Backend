using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Domain.Interfaces.Repositories;

public interface ISubjectRepository
{
    Task<List<Subject>> GetAllAsync(Guid? streamId = null, CancellationToken ct = default);
    Task<Subject?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<SubjectStream?> GetSubjectStreamAsync(Guid subjectId, Guid streamId, CancellationToken ct = default);
    Task AddAsync(Subject subject, CancellationToken ct = default);
    void Update(Subject subject);
    void Delete(Subject subject);
    Task AddSubjectStreamAsync(SubjectStream subjectStream, CancellationToken ct = default);
    void RemoveSubjectStream(SubjectStream subjectStream);
    Task SaveChangesAsync(CancellationToken ct = default);
}
