using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces.Repositories;

namespace ScholarFlow.Infrastructure.Persistence.Repositories;

public sealed class EfQuestionRepository(ApplicationDbContext db) : IQuestionRepository
{
    public Task<List<Question>> GetByPaperIdAsync(Guid paperId, CancellationToken ct = default)
        => db.Questions
            .Where(q => q.PaperId == paperId)
            .Include(q => q.Options)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync(ct);

    public Task<Question?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Questions.FirstOrDefaultAsync(q => q.Id == id, ct);

    public Task<Option?> GetOptionByIdAsync(Guid optionId, CancellationToken ct = default)
        => db.Options.FirstOrDefaultAsync(o => o.Id == optionId, ct);

    public async Task AddAsync(Question question, CancellationToken ct = default)
        => await db.Questions.AddAsync(question, ct);

    public void Update(Question question)
        => db.Questions.Update(question);

    public void Delete(Question question)
        => db.Questions.Remove(question);

    public async Task AddOptionAsync(Option option, CancellationToken ct = default)
        => await db.Options.AddAsync(option, ct);

    public void UpdateOption(Option option)
        => db.Options.Update(option);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
