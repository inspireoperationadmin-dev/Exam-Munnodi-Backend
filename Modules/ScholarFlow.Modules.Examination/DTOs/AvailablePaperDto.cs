namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record AvailablePaperDto(
    Guid Id,
    string Title,
    Guid? SubjectId,
    string? SubjectName,
    int Year,
    string Type,
    string Medium,
    string? Sitting,
    string? OfficialPaperCode,
    decimal NegativeMarkValue,
    int QuestionCount);
