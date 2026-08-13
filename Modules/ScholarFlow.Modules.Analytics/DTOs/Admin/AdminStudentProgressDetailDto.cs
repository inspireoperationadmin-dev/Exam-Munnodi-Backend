namespace ScholarFlow.Modules.Analytics.DTOs.Admin;

public sealed record AdminStudentProgressDetailDto(
    Guid StudentId,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? Medium,
    string? StreamName,
    int? ExamYear,
    DateTime JoinedAt,
    DateTime? LastActiveAt,
    int MockExamsCompleted,
    int TopicExamsCompleted,
    decimal AverageMockScore,
    decimal BestMockScore,
    decimal WeeklyChangePercentage,
    IReadOnlyList<string> Subjects,
    IReadOnlyList<AdminStudentSubjectProgressDto> SubjectProgress,
    IReadOnlyList<AdminStudentFocusSubTopicDto> FocusSubTopics,
    IReadOnlyList<AdminStudentWeeklyTrendDto> WeeklyTrend,
    IReadOnlyList<AdminStudentRecentExamDto> RecentExams);
