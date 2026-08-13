namespace ScholarFlow.Modules.Analytics.DTOs;

public sealed record TopicPerformanceDto(
    Guid TopicId,
    string TopicName,
    int TotalQuestionsInTopic,
    int UniqueQuestionsAttempted,
    int MasteredQuestions,
    int TotalAttempts,
    int CorrectCount,
    decimal CoveragePercentage,
    decimal MasteryPercentage,
    decimal AccuracyPercentage,
    decimal HealthPercentage,
    DateTime LastUpdated);
