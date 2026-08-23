namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record SessionSummaryDto(
    Guid SessionId,
    Guid? PaperId,
    Guid? SubjectId,
    Guid? TopicId,
    string? PaperTitle,
    string? SubjectName,
    DateTime StartTime,
    DateTime LastActivityAt,
    DateTime ServerNow,
    DateTime? ExpiresAt,
    int? TimeLimitMinutes,
    DateTime? EndTime,
    string Status,
    string Mode,
    decimal? Percentage,
    decimal? ObtainedMarks,
    decimal? TotalMarks);
