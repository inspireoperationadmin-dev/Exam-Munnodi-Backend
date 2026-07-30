namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record ActiveSessionDto(
    Guid SessionId,
    string Mode,
    Guid? PaperId,
    Guid? SubjectId,
    string? Title,
    DateTime StartTime,
    DateTime LastActivityAt,
    DateTime? ExpiresAt,
    DateTime ServerNow,
    int AnsweredCount,
    int TotalQuestions,
    int? RemainingSeconds);
