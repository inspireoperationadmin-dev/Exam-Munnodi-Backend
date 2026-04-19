using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Analytics.DTOs;

namespace ScholarFlow.Modules.Analytics.Queries.GetSubjectPerformance;

public sealed class GetSubjectPerformanceQueryHandler(
    ISqlConnectionFactory sql,
    ICurrentUser currentUser)
    : IRequestHandler<GetSubjectPerformanceQuery, List<SubjectPerformanceDto>>
{
    public async Task<List<SubjectPerformanceDto>> Handle(GetSubjectPerformanceQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var rows = await conn.QueryAsync<SubjectPerformanceDto>("""
            SELECT
                ssp.SubjectId,
                s.Name                      AS SubjectName,
                ssp.TotalExams,
                ssp.AverageExamScore        AS AverageScore,
                ssp.BestScore,
                ssp.TotalQuestionsAttempted,
                ssp.OverallCorrectPercentage,
                ssp.StudyStreakDays,
                ssp.LastStudiedAt
            FROM StudentSubjectPerformances ssp
            JOIN Subjects s ON s.Id = ssp.SubjectId
            WHERE ssp.UserId = @UserId
            ORDER BY s.Name
            """,
            new { UserId = currentUser.UserId });

        return rows.ToList();
    }
}
