using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Modules.Academic.Public;

public interface IAcademicApi
{
    Task<bool> PaperExistsAsync(Guid paperId, CancellationToken ct = default);
    Task<AcademicPaperSummary?> GetPaperSummaryAsync(Guid paperId, CancellationToken ct = default);
    Task<IReadOnlyList<AcademicQuestionSummary>> GetQuestionsForExamAsync(Guid paperId, CancellationToken ct = default);
    Task<IReadOnlyList<QuestionPoolItem>> GetQuestionPoolAsync(Guid subjectId, CancellationToken ct = default);
    Task UpdateSystemDifficultyAsync(Guid questionId, SystemDifficultyLevel level, CancellationToken ct = default);
}

public sealed record AcademicPaperSummary(
    Guid     Id,
    bool     IsPublic,
    decimal  NegativeMarkValue,
    Guid?    CreatedByTeacherId,
    int      QuestionCount,
    int      TimeLimit,
    Guid?    SubjectId);               // subject the paper belongs to

public sealed record AcademicQuestionSummary(
    Guid     QuestionId,
    Guid     CorrectOptionId,
    decimal  Marks);                   // ← added

public sealed record QuestionPoolItem(
    Guid                   QuestionId,
    Guid                   SubTopicId,
    Guid                   TopicId,
    Guid                   SubjectId,
    DifficultyLevel?       ManualDifficulty,
    SystemDifficultyLevel? SystemDifficulty)
{
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