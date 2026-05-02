using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.Modules.Academic.Public;
using ScholarFlow.Modules.Examination.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Examination.Commands.EndExamSession;

public sealed class EndExamSessionCommandHandler(
    IExamSessionRepository examRepo,
    IAcademicApi           academicApi,
    ICurrentUser           currentUser)
    : IRequestHandler<EndExamSessionCommand, EndSessionResultDto>
{
    public async Task<EndSessionResultDto> Handle(
        EndExamSessionCommand request, CancellationToken ct)
    {
        var session = await examRepo.GetByIdWithResponsesAsync(request.SessionId, ct)
            ?? throw new NotFoundException("Exam session not found.");

        if (session.UserId != currentUser.UserId)
            throw new ForbiddenException("Access denied.");

        if (session.Status != ExamSessionStatus.InProgress)
            throw new BadRequestException("Session is not in progress.");

        var paper = await academicApi.GetPaperSummaryAsync(session.PaperId!.Value, ct)
            ?? throw new NotFoundException("Paper not found.");

        // Load per-question marks and build lookup dictionary
        var questions = await academicApi.GetQuestionsForExamAsync(session.PaperId!.Value, ct);
        var marksPerQuestion = questions.ToDictionary(
            q => q.QuestionId,
            q => q.Marks);

        // Score using per-question marks — raises ExamSessionCompletedDomainEvent
        var score = session.Complete(paper.NegativeMarkValue, marksPerQuestion);

        await examRepo.SaveChangesAsync(ct);

        int correctCount = session.UserResponses.Count(r => r.IsCorrect);
        int skippedCount = session.UserResponses.Count(r => !r.SelectedOptionId.HasValue);
        int wrongCount   = session.UserResponses.Count - correctCount - skippedCount;
        int timeTaken    = session.Duration.HasValue
            ? (int)session.Duration.Value.TotalSeconds
            : 0;

        return new EndSessionResultDto(
            SessionId:        session.Id,
            ObtainedMarks:    score.ObtainedMarks,
            TotalMarks:       score.TotalMarks,
            Percentage:       score.Percentage,
            IsPassing:        score.IsPassing(),
            CorrectCount:     correctCount,
            WrongCount:       wrongCount,
            SkippedCount:     skippedCount,
            TimeTakenSeconds: timeTaken,
            IsPractice:       session.IsPractice);
    }
}