using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Domain.Interfaces.Repositories;

public interface IExamSessionRepository
{
    /// <summary>Load session with UserResponses + SelectedOption (for scoring in Complete()).</summary>
    Task<ExamSession?> GetByIdWithResponsesAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>Check if the student already has a Completed fixed-exam session for this paper.</summary>
    Task<bool> HasCompletedExamSessionAsync(Guid userId, Guid paperId, CancellationToken ct = default);

    /// <summary>Get a UserResponse row by session + question (for answer submit / flag).</summary>
    Task<UserResponse?> GetResponseAsync(Guid sessionId, Guid questionId, CancellationToken ct = default);

    /// <summary>Check if a question is part of a session (via ExamSessionQuestions).</summary>
    Task<bool> QuestionBelongsToSessionAsync(Guid sessionId, Guid questionId, CancellationToken ct = default);

    Task AddAsync(ExamSession session, CancellationToken ct = default);
    Task AddResponseAsync(UserResponse response, CancellationToken ct = default);
    Task AddSessionQuestionAsync(ExamSessionQuestion question, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
