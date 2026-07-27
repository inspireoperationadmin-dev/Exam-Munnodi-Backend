using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces.Repositories;

namespace ScholarFlow.Infrastructure.Persistence.Repositories;

public sealed class EfStreamRepository(ApplicationDbContext db) : IStreamRepository
{
    public Task<List<AcademicStream>> GetAllAsync(CancellationToken ct = default)
        => db.Streams.OrderBy(s => s.NameEnglish).ToListAsync(ct);

    public Task<AcademicStream?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Streams.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => db.Streams.AnyAsync(s => s.NameEnglish == name, ct);

    public async Task AddAsync(AcademicStream stream, CancellationToken ct = default)
        => await db.Streams.AddAsync(stream, ct);

    public void Update(AcademicStream stream)
        => db.Streams.Update(stream);

    public void Delete(AcademicStream stream)
        => db.Streams.Remove(stream);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
