using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;

namespace ScholarFlow.Modules.Examination.Queries.HasCompletedTest;

public sealed class HasCompletedTestQueryHandler(
    IExamSessionRepository examRepo,
    ICurrentUser currentUser)
    : IRequestHandler<HasCompletedTestQuery, bool>
{
    public Task<bool> Handle(HasCompletedTestQuery request, CancellationToken ct)
        => examRepo.HasCompletedExamSessionAsync(currentUser.UserId, request.PaperId, ct);
}
