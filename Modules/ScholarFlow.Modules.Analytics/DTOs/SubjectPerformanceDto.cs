namespace ScholarFlow.Modules.Analytics.DTOs;

public sealed record SubjectPerformanceDto(
    Guid SubjectId,
    string SubjectName,
    int TotalExams,
    decimal AverageScore,
    decimal BestScore,
    int TotalQuestionsAttempted,
    decimal OverallCorrectPercentage,
    int StudyStreakDays,
    DateTime? LastStudiedAt);
