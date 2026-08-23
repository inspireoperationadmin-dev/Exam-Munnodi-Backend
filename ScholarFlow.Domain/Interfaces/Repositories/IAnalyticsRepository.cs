using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Domain.Interfaces.Repositories;

public interface IAnalyticsRepository
{
    Task<StudentQuestionProgress?> GetQuestionProgressAsync(Guid userId, Guid questionId, CancellationToken ct = default);
    Task<StudentSubTopicPerformance?> GetSubTopicPerformanceAsync(Guid userId, Guid subTopicId, CancellationToken ct = default);
    Task<StudentSubjectPerformance?> GetSubjectPerformanceAsync(Guid userId, Guid subjectId, CancellationToken ct = default);

    Task AddQuestionProgressAsync(StudentQuestionProgress progress, CancellationToken ct = default);
    Task AddSubTopicPerformanceAsync(StudentSubTopicPerformance performance, CancellationToken ct = default);
    Task AddSubjectPerformanceAsync(StudentSubjectPerformance performance, CancellationToken ct = default);
    Task RecalculateSubTopicPerformanceAsync(Guid userId, Guid subTopicId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
