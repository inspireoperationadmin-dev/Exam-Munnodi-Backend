namespace ScholarFlow.Modules.Analytics.DTOs;

public sealed record StudentProgressSummaryDto(
    List<StudentProgressSubjectDto> Subjects,
    List<StudentProgressTopicDto> Topics,
    List<StudentProgressFocusSubTopicDto> NeedsImprovement,
    List<StudentProgressRecentExamDto> RecentExamScores);

public sealed record StudentProgressSubjectDto(
    Guid SubjectId,
    string SubjectName,
    decimal MasteryPercentage,
    int MasteredQuestions,
    int TotalQuestions,
    DateTime? LastStudiedAt);

public sealed record StudentProgressTopicDto(
    Guid SubjectId,
    Guid TopicId,
    string TopicName,
    decimal MasteryPercentage,
    int MasteredQuestions,
    int TotalQuestions,
    int NeedsImprovementSubTopicCount,
    DateTime? LastUpdated);

public sealed record StudentProgressFocusSubTopicDto(
    Guid SubjectId,
    string SubjectName,
    Guid TopicId,
    string TopicName,
    Guid SubTopicId,
    string SubTopicName,
    string Reason);

public sealed record StudentProgressRecentExamDto(
    Guid SessionId,
    Guid? SubjectId,
    string? SubjectName,
    string Mode,
    DateTime? CompletedAt,
    decimal Score);
