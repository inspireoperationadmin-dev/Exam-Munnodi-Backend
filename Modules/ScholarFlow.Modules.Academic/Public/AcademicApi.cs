using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Interfaces;

namespace ScholarFlow.Modules.Academic.Public;

internal sealed class AcademicApi(IApplicationDbContext db) : IAcademicApi
{
    public Task<bool> PaperExistsAsync(Guid paperId, CancellationToken ct = default)
        => db.Papers.AnyAsync(p => p.Id == paperId, ct);

    public async Task<AcademicPaperSummary?> GetPaperSummaryAsync(Guid paperId, CancellationToken ct = default)
    {
        var paper = await db.Papers.FindAsync([paperId], ct);
        if (paper is null) return null;

        var questionCount = await db.Questions.CountAsync(q => q.PaperId == paperId, ct);

        return new AcademicPaperSummary(
            paper.Id,
            paper.IsPublic,
            paper.NegativeMarkValue,
            paper.CreatedByTeacherId,
            questionCount);
    }

    public async Task<IReadOnlyList<AcademicQuestionSummary>> GetQuestionsForExamAsync(
        Guid paperId, CancellationToken ct = default)
    {
        return await db.Questions
            .Where(q => q.PaperId == paperId)
            .Select(q => new AcademicQuestionSummary(
                q.Id,
                db.Options.Where(o => o.QuestionId == q.Id && o.IsCorrect).Select(o => o.Id).First()))
            .ToListAsync(ct);
    }
}
