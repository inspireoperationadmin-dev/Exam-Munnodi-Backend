namespace ScholarFlow.Modules.Academic.DTOs;

public sealed record PaperSummaryDto(
    Guid Id,
    string Title,
    string SubjectName,
    string Type,
    string Medium,
    int Year,
    string? Sitting,
    int QuestionCount,
    bool IsPublic,
    int TimeLimit,
    DateTime CreatedAt)
{
    public bool IsLocked { get; init; }
    public bool CanPractice { get; init; }
    public bool CanUseExamMode { get; init; }
    public string? LockReason { get; init; }
    public string? RequiredPlan { get; init; }
}
