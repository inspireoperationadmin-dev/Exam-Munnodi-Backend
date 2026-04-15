using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces.Repositories;

namespace ScholarFlow.Infrastructure.Persistence.Repositories;

public sealed class EfTeacherProfileRepository(ApplicationDbContext db)
    : ITeacherProfileRepository
{
    public Task<TeacherProfile?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.TeacherProfiles.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<TeacherProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => db.TeacherProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken ct = default)
        => db.TeacherProfiles.AnyAsync(p => p.UserId == userId, ct);

    public async Task AddAsync(TeacherProfile profile, CancellationToken ct = default)
        => await db.TeacherProfiles.AddAsync(profile, ct);

    public void Update(TeacherProfile profile)
        => db.TeacherProfiles.Update(profile);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
