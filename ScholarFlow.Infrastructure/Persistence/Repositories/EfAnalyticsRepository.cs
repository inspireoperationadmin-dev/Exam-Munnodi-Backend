using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces.Repositories;

namespace ScholarFlow.Infrastructure.Persistence.Repositories;

public sealed class EfAnalyticsRepository(ApplicationDbContext db) : IAnalyticsRepository
{
    public Task<StudentQuestionHistory?> GetQuestionHistoryAsync(Guid userId, Guid questionId, CancellationToken ct = default)
        => db.StudentQuestionHistories
            .FirstOrDefaultAsync(h => h.UserId == userId && h.QuestionId == questionId, ct);

    public Task<StudentTopicQuestionProgress?> GetTopicQuestionProgressAsync(Guid userId, Guid questionId, CancellationToken ct = default)
        => db.StudentTopicQuestionProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.QuestionId == questionId, ct);

    public Task<StudentSubTopicPerformance?> GetSubTopicPerformanceAsync(Guid userId, Guid subTopicId, CancellationToken ct = default)
        => db.StudentSubTopicPerformances
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SubTopicId == subTopicId, ct);

    public Task<StudentSubjectPerformance?> GetSubjectPerformanceAsync(Guid userId, Guid subjectId, CancellationToken ct = default)
        => db.StudentSubjectPerformances
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SubjectId == subjectId, ct);

    public async Task AddQuestionHistoryAsync(StudentQuestionHistory history, CancellationToken ct = default)
        => await db.StudentQuestionHistories.AddAsync(history, ct);

    public async Task AddTopicQuestionProgressAsync(StudentTopicQuestionProgress progress, CancellationToken ct = default)
        => await db.StudentTopicQuestionProgresses.AddAsync(progress, ct);

    public async Task AddSubTopicPerformanceAsync(StudentSubTopicPerformance performance, CancellationToken ct = default)
        => await db.StudentSubTopicPerformances.AddAsync(performance, ct);

    public async Task AddSubjectPerformanceAsync(StudentSubjectPerformance performance, CancellationToken ct = default)
        => await db.StudentSubjectPerformances.AddAsync(performance, ct);

    public async Task RecalculateSubTopicPerformanceAsync(Guid userId, Guid subTopicId, CancellationToken ct = default)
    {
        var subTopicInfo = await db.SubTopics
            .Where(st => st.Id == subTopicId)
            .Select(st => new
            {
                st.Id,
                st.TopicId,
                st.Topic.SubjectId
            })
            .FirstOrDefaultAsync(ct);

        if (subTopicInfo is null)
            return;

        var totalQuestions = await db.Questions
            .CountAsync(q => q.SubTopicId == subTopicId, ct);

        var progressRows = await db.StudentTopicQuestionProgresses
            .Where(p => p.UserId == userId && p.SubTopicId == subTopicId)
            .ToListAsync(ct);

        var uniqueAttempted = progressRows.Count;
        var mastered = progressRows.Count(p => p.Status == QuestionProgressStatus.Mastered);
        var attempts = progressRows.Sum(p => p.TimesAttempted);
        var correct = progressRows.Sum(p => p.CorrectCount);

        var performance = await GetSubTopicPerformanceAsync(userId, subTopicId, ct);
        if (performance is null)
        {
            performance = new StudentSubTopicPerformance
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SubTopicId = subTopicInfo.Id,
                TopicId = subTopicInfo.TopicId,
                SubjectId = subTopicInfo.SubjectId
            };

            await AddSubTopicPerformanceAsync(performance, ct);
        }

        performance.TotalQuestionsInSubTopic = totalQuestions;
        performance.UniqueQuestionsAttempted = uniqueAttempted;
        performance.MasteredQuestions = mastered;
        performance.TotalAttempts = attempts;
        performance.CorrectCount = correct;
        performance.CoveragePercentage = CalculatePercentage(uniqueAttempted, totalQuestions);
        performance.MasteryPercentage = CalculatePercentage(mastered, totalQuestions);
        performance.CorrectPercentage = CalculatePercentage(correct, attempts);
        performance.HealthPercentage = CalculatePercentage(mastered, uniqueAttempted);
        performance.LastUpdated = DateTime.UtcNow;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);

    private static decimal CalculatePercentage(int numerator, int denominator)
        => denominator > 0
            ? Math.Round((decimal)numerator / denominator * 100, 2)
            : 0;
}
