namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record StartSessionResultDto(
    Guid SessionId,
    DateTime StartTime,
    bool IsPractice,
    int QuestionCount);
