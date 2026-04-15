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
    DateTime CreatedAt);
