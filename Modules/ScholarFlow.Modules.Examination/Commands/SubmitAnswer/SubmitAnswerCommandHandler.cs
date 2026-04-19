using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Examination.Commands.SubmitAnswer;

public sealed class SubmitAnswerCommandHandler(
    IExamSessionRepository examRepo,
    ICurrentUser currentUser)
    : IRequestHandler<SubmitAnswerCommand>
{
    public async Task Handle(SubmitAnswerCommand request, CancellationToken ct)
    {
        var session = await examRepo.GetByIdWithResponsesAsync(request.SessionId, ct)
            ?? throw new NotFoundException("Exam session not found.");

        if (session.UserId != currentUser.UserId)
            throw new ForbiddenException("Access denied.");

        if (session.Status != ExamSessionStatus.InProgress)
            throw new BadRequestException("Session is not in progress.");

        bool questionBelongs = await examRepo.QuestionBelongsToSessionAsync(
            request.SessionId, request.QuestionId, ct);

        if (!questionBelongs)
            throw new BadRequestException("Question does not belong to this session.");

        var response = await examRepo.GetResponseAsync(request.SessionId, request.QuestionId, ct)
            ?? throw new NotFoundException("Response record not found.");

        response.TimeSpentSeconds += request.TimeSpentSeconds;

        if (request.SelectedOptionId.HasValue)
            response.SelectOption(request.SelectedOptionId.Value);
        else
            response.ClearOption();

        await examRepo.SaveChangesAsync(ct);
    }
}
