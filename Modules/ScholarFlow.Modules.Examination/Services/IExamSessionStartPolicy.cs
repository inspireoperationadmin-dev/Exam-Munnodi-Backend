using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Modules.Examination.Services;

public interface IExamSessionStartPolicy
{
    Task EnsureCanStartAsync(
        ExamSession candidate,
        Guid? replaceSessionId,
        CancellationToken ct);

    Task PersistAsync(
        ExamSession candidate,
        IReadOnlyCollection<UserResponse> responses,
        IReadOnlyCollection<ExamSessionQuestion> sessionQuestions,
        Guid? replaceSessionId,
        CancellationToken ct);
}
