namespace ScholarFlow.Modules.Analytics.DTOs.Admin;

public sealed record AdminStudentRecentExamDto(
    Guid SessionId,
    Guid? SubjectId,
    string? SubjectName,
    string Mode,
    string Status,
    DateTime? CompletedAt,
    decimal FinalScore,
    decimal ObtainedMarks,
    decimal TotalMarks,
    int AnsweredCount,
    int TotalQuestions);
