using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces.Repositories;

namespace ScholarFlow.Infrastructure.Persistence.Repositories;

public sealed class EfStudentProfileRepository(ApplicationDbContext db)
    : IStudentProfileRepository
{
    public Task<StudentProfile?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.StudentProfiles.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<StudentProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => db.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken ct = default)
        => db.StudentProfiles.AnyAsync(p => p.UserId == userId, ct);

    public async Task AddAsync(StudentProfile profile, CancellationToken ct = default)
        => await db.StudentProfiles.AddAsync(profile, ct);

    public void Update(StudentProfile profile)
        => db.StudentProfiles.Update(profile);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
