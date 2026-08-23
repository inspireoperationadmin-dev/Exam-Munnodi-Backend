namespace ScholarFlow.Domain.Interfaces;

/// <summary>
/// Cross-module API contract for Analytics data.
/// Implemented by Analytics module, injected by Examination module.
/// Lives in Domain to break the circular project reference.
/// </summary>
public interface IAnalyticsApi
{
    /// <summary>Returns subtopic performance for a student in a given subject.</summary>
    Task<IReadOnlyList<SubTopicPerformanceSummary>> GetSubTopicPerformancesAsync(
        Guid userId, Guid subjectId, CancellationToken ct = default);

    /// <summary>Returns question IDs the student has seen recently (within the given days window).</summary>
    Task<HashSet<Guid>> GetRecentlySeenQuestionIdsAsync(
        Guid userId, int withinDays, CancellationToken ct = default);

    /// <summary>Returns per-question progress for the requested question IDs.</summary>
    Task<IReadOnlyList<QuestionProgressSummary>> GetQuestionProgressSummariesAsync(
        Guid userId, IReadOnlyCollection<Guid> questionIds, CancellationToken ct = default);
}

public sealed record SubTopicPerformanceSummary(
    Guid SubTopicId,
    int TotalAttempts,
    decimal CorrectPercentage,
    decimal CoveragePercentage,
    decimal MasteryPercentage,
    decimal HealthPercentage);

public sealed record QuestionProgressSummary(
    Guid QuestionId,
    int TimesAttempted,
    int CorrectCount,
    bool LastAnswerCorrect,
    bool LastResponseWasAnswered,
    decimal MasteryScore,
    DateTime LastSeenAt);
