using Microsoft.EntityFrameworkCore;
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

        var ids = await db.StudentQuestionProgresses
            .Where(h => h.UserId == userId && h.LastSeenAt >= cutoff)
            .Select(h => h.QuestionId)
            .ToListAsync(ct);

        return ids.ToHashSet();
    }

    public async Task<IReadOnlyList<QuestionProgressSummary>> GetQuestionProgressSummariesAsync(
        Guid userId, IReadOnlyCollection<Guid> questionIds, CancellationToken ct = default)
    {
        if (questionIds.Count == 0)
            return [];

        var progressRows = await db.StudentQuestionProgresses
            .Where(h => h.UserId == userId && questionIds.Contains(h.QuestionId))
            .ToListAsync(ct);

        return progressRows
            .Select(h => new QuestionProgressSummary(
                h.QuestionId,
                h.TimesAttempted,
                h.CorrectCount,
                h.LastAnswerCorrect,
                h.LastResponseWasAnswered,
                h.MasteryScore,
                h.LastSeenAt))
            .ToList()
            .AsReadOnly();
    }
}
