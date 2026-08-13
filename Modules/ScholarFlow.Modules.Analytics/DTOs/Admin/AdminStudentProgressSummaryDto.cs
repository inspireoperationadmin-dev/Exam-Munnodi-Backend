namespace ScholarFlow.Modules.Analytics.DTOs.Admin;

public sealed record AdminStudentProgressSummaryDto(
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
    string Status,
    IReadOnlyList<string> Subjects);
