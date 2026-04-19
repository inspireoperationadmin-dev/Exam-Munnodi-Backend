using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Examination.Commands.FlagQuestion;

public sealed class FlagQuestionCommandHandler(
    IExamSessionRepository examRepo,
    ICurrentUser currentUser)
    : IRequestHandler<FlagQuestionCommand>
{
    public async Task Handle(FlagQuestionCommand request, CancellationToken ct)
    {
        var session = await examRepo.GetByIdWithResponsesAsync(request.SessionId, ct)
            ?? throw new NotFoundException("Exam session not found.");

        if (session.UserId != currentUser.UserId)
            throw new ForbiddenException("Access denied.");

        if (session.Status != ExamSessionStatus.InProgress)
            throw new BadRequestException("Session is not in progress.");

        var response = await examRepo.GetResponseAsync(request.SessionId, request.QuestionId, ct)
            ?? throw new NotFoundException("Response record not found.");

        if (request.Flagged)
        {
            response.ResponseStatus = ResponseStatus.Flagged;
        }
        else
        {
            // Unflag: restore to Answered or Visited based on whether an option was selected
            response.ResponseStatus = response.SelectedOptionId.HasValue
                ? ResponseStatus.Answered
                : ResponseStatus.Visited;
        }

        await examRepo.SaveChangesAsync(ct);
    }
}
