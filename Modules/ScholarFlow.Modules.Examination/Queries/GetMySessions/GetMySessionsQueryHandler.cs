using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Queries.GetMySessions;

public sealed class GetMySessionsQueryHandler(
    ISqlConnectionFactory sql,
    ICurrentUser currentUser)
    : IRequestHandler<GetMySessionsQuery, List<SessionSummaryDto>>
{
    public async Task<List<SessionSummaryDto>> Handle(GetMySessionsQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var rows = await conn.QueryAsync<SessionSummaryRow>("""
            SELECT
                es.Id           AS SessionId,
                es.PaperId,
                p.Title         AS PaperTitle,
                s.Name          AS SubjectName,
                es.StartTime,
                es.EndTime,
                es.Status,
                es.IsPractice,
                es.FinalScore   AS Percentage,
                es.ObtainedMarks,
                es.TotalMarks
            FROM ExamSessions es
            LEFT JOIN Papers p  ON p.Id = es.PaperId
            LEFT JOIN Subjects s ON s.Id = p.SubjectId
            WHERE es.UserId = @UserId
              AND (@PaperId    IS NULL OR es.PaperId    = @PaperId)
              AND (@IsPractice IS NULL OR es.IsPractice = @IsPractice)
            ORDER BY es.StartTime DESC
            """,
            new
            {
                UserId     = currentUser.UserId,
                request.PaperId,
                request.IsPractice
            });

        return rows.Select(r => new SessionSummaryDto(
            SessionId:    r.SessionId,
            PaperId:      r.PaperId,
            PaperTitle:   r.PaperTitle,
            SubjectName:  r.SubjectName,
            StartTime:    r.StartTime,
            EndTime:      r.EndTime,
            Status:       r.Status,
            IsPractice:   r.IsPractice,
            Percentage:   r.Percentage,
            ObtainedMarks: r.ObtainedMarks,
            TotalMarks:   r.TotalMarks))
            .ToList();
    }

    private sealed record SessionSummaryRow(
        Guid SessionId,
        Guid? PaperId,
        string? PaperTitle,
        string? SubjectName,
        DateTime StartTime,
        DateTime? EndTime,
        string Status,
        bool IsPractice,
        decimal? Percentage,
        decimal? ObtainedMarks,
        decimal? TotalMarks);
}
