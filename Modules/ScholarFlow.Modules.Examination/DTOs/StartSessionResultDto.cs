namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record StartSessionResultDto(
    Guid SessionId,
    DateTime StartTime,
    DateTime ServerNow,
    DateTime? ExpiresAt,
    int? TimeLimitMinutes,
    string Mode,
    int QuestionCount,
    List<ExamQuestionDto> Questions); // <-- Added to carry the question layout [1]
