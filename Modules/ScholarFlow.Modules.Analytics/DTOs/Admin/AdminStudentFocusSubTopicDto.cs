namespace ScholarFlow.Modules.Analytics.DTOs.Admin;

public sealed record AdminStudentFocusSubTopicDto(
    Guid SubjectId,
    string SubjectName,
    Guid TopicId,
    string TopicName,
    Guid SubTopicId,
    string SubTopicName,
    decimal MasteryPercentage,
    decimal AccuracyPercentage,
    decimal HealthPercentage,
    int TotalAttempts,
    DateTime LastUpdated);
