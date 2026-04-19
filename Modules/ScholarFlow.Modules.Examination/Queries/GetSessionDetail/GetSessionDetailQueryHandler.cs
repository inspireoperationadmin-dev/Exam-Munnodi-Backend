using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Examination.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Examination.Queries.GetSessionDetail;

public sealed class GetSessionDetailQueryHandler(
    ISqlConnectionFactory sql,
    ICurrentUser currentUser)
    : IRequestHandler<GetSessionDetailQuery, SessionDetailDto>
{
    public async Task<SessionDetailDto> Handle(GetSessionDetailQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        // Load session header
        var session = await conn.QuerySingleOrDefaultAsync<SessionHeaderRow>("""
            SELECT
                es.Id           AS SessionId,
                es.UserId,
                es.PaperId,
                p.Title         AS PaperTitle,
                es.StartTime,
                es.EndTime,
                es.Status,
                es.IsPractice,
                es.ObtainedMarks,
                es.TotalMarks,
                es.FinalScore   AS Percentage
            FROM ExamSessions es
            LEFT JOIN Papers p ON p.Id = es.PaperId
            WHERE es.Id = @SessionId
            """,
            new { request.SessionId });

        if (session is null)
            throw new NotFoundException("Session not found.");

        if (session.UserId != currentUser.UserId)
            throw new ForbiddenException("Access denied.");

        // Load responses with order from ExamSessionQuestions
        var responses = await conn.QueryAsync<SessionResponseRow>("""
            SELECT
                ur.QuestionId,
                esq.OrderIndex,
                ur.SelectedOptionId,
                ur.IsCorrect,
                ur.MarksAwarded,
                ur.ResponseStatus
            FROM UserResponses ur
            JOIN ExamSessionQuestions esq
                ON esq.SessionId  = ur.SessionId
               AND esq.QuestionId = ur.QuestionId
            WHERE ur.SessionId = @SessionId
            ORDER BY esq.OrderIndex
            """,
            new { request.SessionId });

        var responseList = responses.Select(r => new SessionResponseDto(
            QuestionId:      r.QuestionId,
            OrderIndex:      r.OrderIndex,
            SelectedOptionId: r.SelectedOptionId,
            IsCorrect:       r.IsCorrect,
            MarksAwarded:    r.MarksAwarded,
            ResponseStatus:  r.ResponseStatus))
            .ToList();

        int correctCount = responseList.Count(r => r.IsCorrect);
        int skippedCount = responseList.Count(r => r.SelectedOptionId is null);
        int wrongCount   = responseList.Count - correctCount - skippedCount;

        return new SessionDetailDto(
            SessionId:    session.SessionId,
            PaperId:      session.PaperId,
            PaperTitle:   session.PaperTitle,
            StartTime:    session.StartTime,
            EndTime:      session.EndTime,
            Status:       session.Status,
            IsPractice:   session.IsPractice,
            ObtainedMarks: session.ObtainedMarks,
            TotalMarks:   session.TotalMarks,
            Percentage:   session.Percentage,
            CorrectCount: correctCount,
            WrongCount:   wrongCount,
            SkippedCount: skippedCount,
            Responses:    responseList);
    }

    private sealed record SessionHeaderRow(
        Guid SessionId,
        Guid UserId,
        Guid? PaperId,
        string? PaperTitle,
        DateTime StartTime,
        DateTime? EndTime,
        string Status,
        bool IsPractice,
        decimal ObtainedMarks,
        decimal TotalMarks,
        decimal Percentage);

    private sealed record SessionResponseRow(
        Guid QuestionId,
        int OrderIndex,
        Guid? SelectedOptionId,
        bool IsCorrect,
        decimal MarksAwarded,
        string ResponseStatus);
}
