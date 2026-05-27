namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record StartSessionResultDto(
    Guid SessionId,
    DateTime StartTime,
    bool IsPractice,
    int QuestionCount,
    List<ExamQuestionDto> Questions); // <-- Added to carry the question layout [1]