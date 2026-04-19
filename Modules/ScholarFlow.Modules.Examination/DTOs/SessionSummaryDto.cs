namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record SessionSummaryDto(
    Guid SessionId,
    Guid? PaperId,
    string? PaperTitle,
    string? SubjectName,
    DateTime StartTime,
    DateTime? EndTime,
    string Status,
    bool IsPractice,
    decimal? Percentage,
    decimal? ObtainedMarks,
    decimal? TotalMarks);
