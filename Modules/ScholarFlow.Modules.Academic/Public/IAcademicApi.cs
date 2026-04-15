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
