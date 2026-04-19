namespace ScholarFlow.Modules.Analytics.DTOs;

public sealed record ExamHistoryDto(
    Guid SessionId,
    string? PaperTitle,
    DateTime Date,
    decimal Score,
    decimal ObtainedMarks,
    decimal TotalMarks);
