namespace ScholarFlow.Domain.Interfaces;

/// <summary>
/// Cross-module API contract for Examination data.
/// Implemented by Examination module, injected by Analytics module.
/// Lives in Domain to break the circular project reference.
/// </summary>
public interface IExaminationApi
{
    /// <summary>
    /// Returns per-question response data for a completed session.
    /// Used by Analytics module to update SubTopic and Question performance.
    /// </summary>
    Task<IReadOnlyList<SessionResponseSummary>> GetSessionResponsesAsync(Guid sessionId, CancellationToken ct = default);
}

public sealed record SessionResponseSummary(
    Guid QuestionId,
    Guid SubTopicId,
    Guid TopicId,
    Guid SubjectId,
    bool IsCorrect,
    bool WasAnswered);
