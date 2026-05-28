using Dapper;
using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.Modules.Examination.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Examination.Commands.EndExamSession;

public sealed class EndExamSessionCommandHandler(
    IExamSessionRepository examRepo,
    ISqlConnectionFactory  sql,             // <-- Injected for high-performance Dapper lookup [1]
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

        // 3. Query the Marks and CorrectOptionId directly from the database for all questions in this session [1]
        var questionIds = session.UserResponses.Select(r => r.QuestionId).ToList();

        using var conn = sql.CreateConnection();
        var gradingDetails = (await conn.QueryAsync<GradingRow>("""
            SELECT 
                q.Id AS QuestionId,
                q.Marks,
                o.Id AS CorrectOptionId
            FROM Questions q
            LEFT JOIN Options o ON o.QuestionId = q.Id AND o.IsCorrect = 1
            WHERE q.Id IN @QuestionIds
              AND q.IsDeleted = 0
            """,
            new { QuestionIds = questionIds })).ToList();

        // 4. Construct lookup dictionaries for the scoring engine [1]
        var marksPerQuestion = gradingDetails.ToDictionary(
            g => g.QuestionId,
            g => g.Marks);

        var correctOptionPerQuestion = gradingDetails.ToDictionary(
            g => g.QuestionId,
            g => g.CorrectOptionId ?? Guid.Empty); // Fallback if no correct option is configured [1]

        // 5. Score using per-question marks and correct options mapping [1]
        var score = session.Complete(marksPerQuestion, correctOptionPerQuestion);

        // 6. Commit all changed states atomically to Azure SQL
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

    private sealed class GradingRow
    {
        public Guid QuestionId { get; set; }
        public decimal Marks { get; set; }
        public Guid? CorrectOptionId { get; set; }
    }
}