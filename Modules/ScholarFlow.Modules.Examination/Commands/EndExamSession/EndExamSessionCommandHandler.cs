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
        // 1. Load session with UserResponses
        var session = await examRepo.GetByIdWithResponsesAsync(request.SessionId, ct)
            ?? throw new NotFoundException("Exam session not found.");

        if (session.UserId != currentUser.UserId)
            throw new ForbiddenException("Access denied.");

        if (session.Status != ExamSessionStatus.InProgress)
            throw new BadRequestException("Session is not in progress.");

        var paper = await academicApi.GetPaperSummaryAsync(session.PaperId!.Value, ct)
            ?? throw new NotFoundException("Paper not found.");

        // 2. Map and update the user's responses locally [1]
        foreach (var submitted in request.SubmittedAnswers)
        {
            var response = session.UserResponses.FirstOrDefault(r => r.QuestionId == submitted.QuestionId);
            if (response is null)
            {
                continue;
            }

            if (submitted.SelectedOptionId.HasValue)
            {
                response.SelectOption(submitted.SelectedOptionId.Value);
            }
            else
            {
                response.ClearOption();
            }

            response.TimeSpentSeconds = submitted.TimeSpentSeconds;
        }

        // 3. Load per-question marks and correct options lookup mapping [1]
        var questions = await academicApi.GetQuestionsForExamAsync(session.PaperId!.Value, ct);
        
        var marksPerQuestion = questions.ToDictionary(
            q => q.QuestionId,
            q => q.Marks);

        var correctOptionPerQuestion = questions.ToDictionary(
            q => q.QuestionId,
            q => q.CorrectOptionId); // <-- Added [1]

        // 4. Score using per-question marks and correct options mapping [1]
        var score = session.Complete(marksPerQuestion, correctOptionPerQuestion);

        // 5. Commit all changed states atomically to Azure SQL
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