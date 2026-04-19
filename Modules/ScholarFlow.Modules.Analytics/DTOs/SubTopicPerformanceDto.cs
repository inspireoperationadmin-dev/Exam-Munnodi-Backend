namespace ScholarFlow.Modules.Analytics.DTOs;

public sealed record SubTopicPerformanceDto(
    Guid SubTopicId,
    string SubTopicName,
    string TopicName,
    int TotalAttempts,
    int CorrectCount,
    decimal CorrectPercentage,
    DateTime LastUpdated);
