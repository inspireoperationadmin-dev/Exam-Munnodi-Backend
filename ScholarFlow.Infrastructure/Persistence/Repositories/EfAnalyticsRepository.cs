using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces.Repositories;

namespace ScholarFlow.Infrastructure.Persistence.Repositories;

public sealed class EfAnalyticsRepository(ApplicationDbContext db) : IAnalyticsRepository
{
    public Task<StudentQuestionHistory?> GetQuestionHistoryAsync(Guid userId, Guid questionId, CancellationToken ct = default)
        => db.StudentQuestionHistories
            .FirstOrDefaultAsync(h => h.UserId == userId && h.QuestionId == questionId, ct);

    public Task<StudentSubTopicPerformance?> GetSubTopicPerformanceAsync(Guid userId, Guid subTopicId, CancellationToken ct = default)
        => db.StudentSubTopicPerformances
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SubTopicId == subTopicId, ct);

    public Task<StudentSubjectPerformance?> GetSubjectPerformanceAsync(Guid userId, Guid subjectId, CancellationToken ct = default)
        => db.StudentSubjectPerformances
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SubjectId == subjectId, ct);

    public async Task AddQuestionHistoryAsync(StudentQuestionHistory history, CancellationToken ct = default)
        => await db.StudentQuestionHistories.AddAsync(history, ct);

    public async Task AddSubTopicPerformanceAsync(StudentSubTopicPerformance performance, CancellationToken ct = default)
        => await db.StudentSubTopicPerformances.AddAsync(performance, ct);

    public async Task AddSubjectPerformanceAsync(StudentSubjectPerformance performance, CancellationToken ct = default)
        => await db.StudentSubjectPerformances.AddAsync(performance, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
