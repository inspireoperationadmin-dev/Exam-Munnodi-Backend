using Dapper;
using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Examination.Commands.AbandonExamSession;

public sealed class AbandonExamSessionCommandHandler(
    IExamSessionRepository examRepo,
    ISqlConnectionFactory sql,
    ICurrentUser currentUser)
    : IRequestHandler<AbandonExamSessionCommand>
{
    public async Task Handle(AbandonExamSessionCommand request, CancellationToken ct)
    {
        var session = await examRepo.GetByIdWithResponsesAsync(request.SessionId, ct)
            ?? throw new NotFoundException("Exam session not found.");

        if (session.UserId != currentUser.UserId)
            throw new ForbiddenException("Access denied.");

        if (session.Status is ExamSessionStatus.Completed or ExamSessionStatus.TimedOut or ExamSessionStatus.Abandoned)
            return;

        if (session.Status != ExamSessionStatus.InProgress)
            throw new BadRequestException("Session is not in progress.");

        if (session.HasExpired(DateTime.UtcNow))
        {
            await CompleteTimedOutAsync(session, ct);
            return;
        }

        session.Abandon();
        await examRepo.SaveChangesAsync(ct);
    }

    private async Task CompleteTimedOutAsync(ExamSession session, CancellationToken ct)
    {
        var questionIds = session.UserResponses.Select(response => response.QuestionId).ToList();

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
            """, new { QuestionIds = questionIds }))
            .ToList();

        var marksPerQuestion = gradingDetails.ToDictionary(row => row.QuestionId, row => row.Marks);
        var correctOptionPerQuestion = gradingDetails.ToDictionary(
            row => row.QuestionId,
            row => row.CorrectOptionId ?? Guid.Empty);

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
