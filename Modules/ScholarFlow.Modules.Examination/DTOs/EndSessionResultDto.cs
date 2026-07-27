namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record EndSessionResultDto(
    Guid SessionId,
    decimal ObtainedMarks,
    decimal TotalMarks,
    decimal Percentage,
    bool IsPassing,
    int CorrectCount,
    int WrongCount,
    int SkippedCount,
    int TimeTakenSeconds,
    string Mode,
    string Status);
