namespace ScholarFlow.Modules.Academic.DTOs;

public sealed record PaperDetailDto(
    Guid Id,
    string Title,
    Guid? SubjectId,
    string? SubjectName,
    string Type,
    string Medium,
    int Year,
    string? Sitting,
    decimal NegativeMarkValue,
    bool IsPublic,
    int TimeLimit,
    string? OfficialPaperCode,
    int QuestionCount,
    Guid? CreatedByTeacherId,
    DateTime CreatedAt)
{
    public bool IsLocked { get; init; }
    public bool CanPractice { get; init; }
    public bool CanUseExamMode { get; init; }
    public string? LockReason { get; init; }
    public string? RequiredPlan { get; init; }
}
