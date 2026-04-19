using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Modules.Academic.Public;

/// <summary>
/// Public API exposed by the Academic module.
/// Examination module injects this — never touches Academic DbSets directly.
/// </summary>
public interface IAcademicApi
{
    Task<bool> PaperExistsAsync(Guid paperId, CancellationToken ct = default);
    Task<AcademicPaperSummary?> GetPaperSummaryAsync(Guid paperId, CancellationToken ct = default);
    Task<IReadOnlyList<AcademicQuestionSummary>> GetQuestionsForExamAsync(Guid paperId, CancellationToken ct = default);

    /// <summary>
    /// Returns all active questions for a subject with difficulty info.
    /// Used by Examination module for personalized exam generation.
    /// </summary>
    Task<IReadOnlyList<QuestionPoolItem>> GetQuestionPoolAsync(Guid subjectId, CancellationToken ct = default);

    /// <summary>
    /// Updates the system-calculated difficulty on a question.
    /// Called by Analytics module after accumulating ≥5 student attempt data points.
    /// </summary>
    Task UpdateSystemDifficultyAsync(Guid questionId, SystemDifficultyLevel level, CancellationToken ct = default);
}

public sealed record AcademicPaperSummary(
    Guid Id,
    bool IsPublic,
    decimal NegativeMarkValue,
    Guid? CreatedByTeacherId,
    int QuestionCount);

public sealed record AcademicQuestionSummary(
    Guid QuestionId,
    Guid CorrectOptionId);

public sealed record QuestionPoolItem(
    Guid QuestionId,
    Guid SubTopicId,
    Guid TopicId,
    Guid SubjectId,
    DifficultyLevel? ManualDifficulty,
    SystemDifficultyLevel? SystemDifficulty)
{
    /// <summary>Effective difficulty for question selection — Manual takes priority.</summary>
    public SystemDifficultyLevel EffectiveDifficulty =>
        ManualDifficulty switch
        {
            DifficultyLevel.DirectRecall => SystemDifficultyLevel.Easy,
            DifficultyLevel.Conceptual   => SystemDifficultyLevel.Medium,
            DifficultyLevel.Calculation  => SystemDifficultyLevel.Medium,
            DifficultyLevel.Analytical   => SystemDifficultyLevel.Hard,
            _                            => SystemDifficulty ?? SystemDifficultyLevel.Medium
        };
}
