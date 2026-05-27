using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces.Repositories;

namespace ScholarFlow.Infrastructure.Persistence.Repositories;

public sealed class EfExplanationRepository(ApplicationDbContext db) : IExplanationRepository
{
    // Added: Query explanations in bulk including their ordered sections [1]
    public Task<List<Explanation>> GetByQuestionIdsAsync(List<Guid> questionIds, CancellationToken ct = default)
        => db.Explanations
            .Include(e => e.Sections)
            .Where(e => questionIds.Contains(e.QuestionId))
            .ToListAsync(ct);

    public Task<Explanation?> GetByQuestionIdAsync(Guid questionId, CancellationToken ct = default)
        => db.Explanations
            .Include(e => e.Sections)
            .FirstOrDefaultAsync(e => e.QuestionId == questionId, ct);

    public Task<Explanation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Explanations.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<ExplanationSection?> GetSectionByIdAsync(Guid sectionId, CancellationToken ct = default)
        => db.ExplanationSections.FirstOrDefaultAsync(s => s.Id == sectionId, ct);

    public async Task AddAsync(Explanation explanation, CancellationToken ct = default)
        => await db.Explanations.AddAsync(explanation, ct);

    public void Update(Explanation explanation)
        => db.Explanations.Update(explanation);

    public void Delete(Explanation explanation)
        => db.Explanations.Remove(explanation);

    public async Task AddSectionAsync(ExplanationSection section, CancellationToken ct = default)
        => await db.ExplanationSections.AddAsync(section, ct);

    public void UpdateSection(ExplanationSection section)
        => db.ExplanationSections.Update(section);

    public void DeleteSection(ExplanationSection section)
        => db.ExplanationSections.Remove(section);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}