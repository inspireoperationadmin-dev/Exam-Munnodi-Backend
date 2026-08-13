namespace ScholarFlow.Modules.Analytics.DTOs;

public sealed record SubjectPerformanceDto(
    Guid SubjectId,
    string SubjectName,
    int TotalQuestionsInSubject,
    int UniqueQuestionsAttempted,
    int MasteredQuestions,
    int TotalExams,
    decimal AverageScore,
    decimal BestScore,
    int TotalQuestionsAttempted,
    decimal OverallCorrectPercentage,
    decimal CoveragePercentage,
    decimal MasteryPercentage,
    decimal AccuracyPercentage,
    decimal ReadinessPercentage,
    int StudyStreakDays,
    DateTime? LastStudiedAt);
