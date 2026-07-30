namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record ResumeSessionResultDto(
    bool IsResumable,
    string Status,
    string? Title,
    StartSessionResultDto? Session,
    List<ResumeAnswerDto> Responses,
    int AnsweredCount,
    int TotalQuestions,
    int? RemainingSeconds);
