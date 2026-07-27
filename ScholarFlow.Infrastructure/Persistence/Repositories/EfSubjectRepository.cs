using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces.Repositories;

namespace ScholarFlow.Infrastructure.Persistence.Repositories;

public sealed class EfSubjectRepository(ApplicationDbContext db) : ISubjectRepository
{
    public Task<List<Subject>> GetAllAsync(Guid? streamId = null, CancellationToken ct = default)
    {
        var query = db.Subjects.AsQueryable();

        if (streamId.HasValue)
            query = query.Where(s => s.SubjectStreams.Any(ss => ss.StreamId == streamId.Value));

        return query.OrderBy(s => s.NameEnglish).ToListAsync(ct);
    }

    public Task<Subject?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Subjects
            .Include(s => s.SubjectStreams)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => db.Subjects.AnyAsync(s => s.NameEnglish == name, ct);

    public Task<SubjectStream?> GetSubjectStreamAsync(Guid subjectId, Guid streamId, CancellationToken ct = default)
        => db.SubjectStreams.FirstOrDefaultAsync(ss => ss.SubjectId == subjectId && ss.StreamId == streamId, ct);

    public async Task AddAsync(Subject subject, CancellationToken ct = default)
        => await db.Subjects.AddAsync(subject, ct);

    public void Update(Subject subject)
        => db.Subjects.Update(subject);

    public void Delete(Subject subject)
        => db.Subjects.Remove(subject);

    public async Task AddSubjectStreamAsync(SubjectStream subjectStream, CancellationToken ct = default)
        => await db.SubjectStreams.AddAsync(subjectStream, ct);

    public void RemoveSubjectStream(SubjectStream subjectStream)
        => db.SubjectStreams.Remove(subjectStream);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
