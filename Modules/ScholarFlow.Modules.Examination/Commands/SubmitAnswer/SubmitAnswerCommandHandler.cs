using MediatR;
using Dapper;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Examination.Commands.SubmitAnswer;

public sealed class SubmitAnswerCommandHandler(
    IExamSessionRepository examRepo,
    ISqlConnectionFactory sql,
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

        if (session.HasExpired(DateTime.UtcNow))
        {
            await CompleteTimedOutAsync(session, ct);
            throw new BadRequestException("Exam time has expired.");
        }

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

    private async Task CompleteTimedOutAsync(ScholarFlow.Domain.Entities.ExamSession session, CancellationToken ct)
    {
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

        var marksPerQuestion = gradingDetails.ToDictionary(g => g.QuestionId, g => g.Marks);
        var correctOptionPerQuestion = gradingDetails.ToDictionary(
            g => g.QuestionId,
            g => g.CorrectOptionId ?? Guid.Empty);

        session.Complete(marksPerQuestion, correctOptionPerQuestion, ExamSessionStatus.TimedOut);
        await examRepo.SaveChangesAsync(ct);
    }

    private sealed class GradingRow
    {
        public Guid QuestionId { get; set; }
        public decimal Marks { get; set; }
        public Guid? CorrectOptionId { get; set; }
    }
}
