using Dapper;
using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Analytics.DTOs;

namespace ScholarFlow.Modules.Analytics.Queries.GetExamHistory;

public sealed class GetExamHistoryQueryHandler(
    ISqlConnectionFactory sql,
    ICurrentUser currentUser,
    ISubscriptionsApi subscriptionsApi)
    : IRequestHandler<GetExamHistoryQuery, List<ExamHistoryDto>>
{
    public async Task<List<ExamHistoryDto>> Handle(GetExamHistoryQuery request, CancellationToken ct)
    {
        await subscriptionsApi.EnsureProgressAccessAsync(
            currentUser.UserId,
            ProgressAccessLevel.Full,
            ct);

        using var conn = sql.CreateConnection();

        var rows = await conn.QueryAsync<ExamHistoryDto>("""
            SELECT
                es.Id           AS SessionId,
                p.Title         AS PaperTitle,
                es.EndTime      AS Date,
                es.FinalScore   AS Score,
                es.ObtainedMarks,
                es.TotalMarks
            FROM ExamSessions es
            LEFT JOIN Papers p ON p.Id = es.PaperId
            WHERE es.UserId     = @UserId
              AND es.Status     = 'Completed'
              AND es.Mode       = 'MockExam'
              AND (
                    p.SubjectId = @SubjectId
                 OR es.SubjectId = @SubjectId
              )
            ORDER BY es.EndTime ASC
            """,
            new { UserId = currentUser.UserId, request.SubjectId });

        return rows.ToList();
    }
}
