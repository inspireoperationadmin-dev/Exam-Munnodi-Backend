using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces.Repositories;

namespace ScholarFlow.Infrastructure.Persistence.Repositories;

public sealed class EfPaperRepository(ApplicationDbContext db) : IPaperRepository
{
    public Task<List<Paper>> GetAllAsync(
        Guid? subjectId, PaperType? type, PaperMedium? medium, int? year,
        CancellationToken ct = default)
    {
        var query = db.Papers.AsQueryable();

        if (subjectId.HasValue) query = query.Where(p => p.SubjectId == subjectId);
        if (type.HasValue)      query = query.Where(p => p.Type == type);
        if (medium.HasValue)    query = query.Where(p => p.Medium == medium);
        if (year.HasValue)      query = query.Where(p => p.Year == year);

        return query
            .OrderByDescending(p => p.Year)
            .ThenBy(p => p.Title)
            .ToListAsync(ct);
    }

    public Task<Paper?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Papers.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExistsByCompositeKeyAsync(
        string title, Guid? subjectId, int year, PaperType type, PaperMedium medium,
        CancellationToken ct = default)
        => db.Papers.AnyAsync(
            p => p.Title      == title
              && p.SubjectId  == subjectId
              && p.Year       == year
              && p.Type       == type
              && p.Medium     == medium,
            ct);

    public Task<bool> ExistsByCompositeKeyExcludingIdAsync(
    Guid excludeId, string title, Guid? subjectId, int year, PaperType type, PaperMedium medium,
    CancellationToken ct = default)
    => db.Papers.AnyAsync(
        p => p.Id       != excludeId
          && p.Title    == title
          && p.SubjectId == subjectId
          && p.Year     == year
          && p.Type     == type
          && p.Medium   == medium,
        ct);

        
    public Task<bool> IsTeacherOwnerAsync(Guid paperId, Guid userId, CancellationToken ct = default)
        => db.Papers
            .Where(p => p.Id == paperId)
            .Join(db.TeacherProfiles,
                p  => p.CreatedByTeacherId,
                tp => tp.Id,
                (p, tp) => tp.UserId)
            .AnyAsync(uid => uid == userId, ct);

    public Task<Guid?> GetTeacherProfileIdByUserIdAsync(Guid userId, CancellationToken ct = default)
        => db.TeacherProfiles
            .Where(tp => tp.UserId == userId)
            .Select(tp => (Guid?)tp.Id)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(Paper paper, CancellationToken ct = default)
        => await db.Papers.AddAsync(paper, ct);

    public void Update(Paper paper)
        => db.Papers.Update(paper);

    public void Delete(Paper paper)
        => db.Papers.Remove(paper);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}