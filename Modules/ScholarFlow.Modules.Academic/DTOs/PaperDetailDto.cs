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
    string? OfficialPaperCode,
    int QuestionCount,
    Guid? CreatedByTeacherId,
    DateTime CreatedAt);
