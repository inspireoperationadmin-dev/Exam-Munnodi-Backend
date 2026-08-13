namespace ScholarFlow.Modules.Analytics.DTOs.Admin;

public sealed record AdminStudentSubjectProgressDto(
    Guid SubjectId,
    string SubjectName,
    int TotalQuestionsInSubject,
    int UniqueQuestionsAttempted,
    int MasteredQuestions,
    int MockExamsCompleted,
    decimal AverageMockScore,
    decimal BestMockScore,
    decimal CoveragePercentage,
    decimal MasteryPercentage,
    decimal AccuracyPercentage,
    decimal ReadinessPercentage,
    decimal WeeklyChangePercentage,
    DateTime? LastStudiedAt);
