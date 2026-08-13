using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;

namespace ScholarFlow.Modules.Analytics.Public;

internal sealed class AnalyticsApi(IApplicationDbContext db) : IAnalyticsApi
{
    public async Task<IReadOnlyList<SubTopicPerformanceSummary>> GetSubTopicPerformancesAsync(
        Guid userId, Guid subjectId, CancellationToken ct = default)
    {
        var result = await db.StudentSubTopicPerformances
            .Where(p => p.UserId == userId && p.SubjectId == subjectId)
            .Select(p => new SubTopicPerformanceSummary(
                p.SubTopicId,
                p.TotalAttempts,
                p.CorrectPercentage,
                p.CoveragePercentage,
                p.MasteryPercentage,
                p.HealthPercentage))
            .ToListAsync(ct);

        return result.AsReadOnly();
    }

    public async Task<HashSet<Guid>> GetRecentlySeenQuestionIdsAsync(
        Guid userId, int withinDays, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-withinDays);

        var ids = await db.StudentQuestionHistories
            .Where(h => h.UserId == userId && h.LastSeenAt >= cutoff)
            .Select(h => h.QuestionId)
            .ToListAsync(ct);

        return ids.ToHashSet();
    }

    public async Task<IReadOnlyList<QuestionHistorySummary>> GetQuestionHistoriesAsync(
        Guid userId, IReadOnlyCollection<Guid> questionIds, CancellationToken ct = default)
    {
        if (questionIds.Count == 0)
            return [];

        var histories = await db.StudentQuestionHistories
            .Where(h => h.UserId == userId && questionIds.Contains(h.QuestionId))
            .ToListAsync(ct);

        var latestResponses = await db.UserResponses
            .Where(r => r.Session.UserId == userId
                     && questionIds.Contains(r.QuestionId)
                     && r.Session.Mode == ExamMode.MockExam
                     && (r.Session.Status == ExamSessionStatus.Completed
                      || r.Session.Status == ExamSessionStatus.TimedOut))
            .Select(r => new
            {
                r.QuestionId,
                r.SelectedOptionId,
                r.OrderIndex,
                r.Session.StartTime,
                r.Session.EndTime
            })
            .ToListAsync(ct);

        var latestAnswerState = latestResponses
            .GroupBy(r => r.QuestionId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(r => r.EndTime ?? r.StartTime)
                    .ThenByDescending(r => r.OrderIndex)
                    .First()
                    .SelectedOptionId.HasValue);

        return histories
            .Select(h => new QuestionHistorySummary(
                h.QuestionId,
                h.TimesAttempted,
                h.CorrectCount,
                h.LastAnswerCorrect,
                latestAnswerState.GetValueOrDefault(h.QuestionId, true),
                h.LastSeenAt))
            .ToList()
            .AsReadOnly();
    }
}
