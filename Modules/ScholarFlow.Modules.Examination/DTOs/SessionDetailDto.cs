namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record SessionDetailDto(
    Guid SessionId,
    Guid? PaperId,
    Guid? SubjectId,
    string? PaperTitle,
    DateTime StartTime,
    DateTime ServerNow,
    DateTime? ExpiresAt,
    int? TimeLimitMinutes,
    DateTime? EndTime,
    string Status,
    string Mode,
    decimal ObtainedMarks,
    decimal TotalMarks,
    decimal Percentage,
    int CorrectCount,
    int WrongCount,
    int SkippedCount,
    List<SessionResponseDto> Responses);
