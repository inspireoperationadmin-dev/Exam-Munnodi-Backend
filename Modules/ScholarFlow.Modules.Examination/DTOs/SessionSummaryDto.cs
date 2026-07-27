namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record SessionSummaryDto(
    Guid SessionId,
    Guid? PaperId,
    string? PaperTitle,
    string? SubjectName,
    DateTime StartTime,
    DateTime ServerNow,
    DateTime? ExpiresAt,
    int? TimeLimitMinutes,
    DateTime? EndTime,
    string Status,
    string Mode,
    decimal? Percentage,
    decimal? ObtainedMarks,
    decimal? TotalMarks);
