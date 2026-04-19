namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record SessionDetailDto(
    Guid SessionId,
    Guid? PaperId,
    string? PaperTitle,
    DateTime StartTime,
    DateTime? EndTime,
    string Status,
    bool IsPractice,
    decimal ObtainedMarks,
    decimal TotalMarks,
    decimal Percentage,
    int CorrectCount,
    int WrongCount,
    int SkippedCount,
    List<SessionResponseDto> Responses);
