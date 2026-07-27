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
            -- Primary: rows already tracked in StudentSubjectPerformances
            SELECT
                ssp.SubjectId,
                s.NameEnglish               AS SubjectName,
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

            UNION ALL

            -- Fallback: subjects with subtopic data but no StudentSubjectPerformances row yet
            -- (sessions completed before the SubjectId pipeline fix)
            SELECT
                sts.SubjectId,
                s.NameEnglish                                                       AS SubjectName,
                COUNT(DISTINCT es_p.Id)                                             AS TotalExams,
                ISNULL(CAST(AVG(es_p.FinalScore)  AS decimal(18,2)), 0)             AS AverageScore,
                ISNULL(CAST(MAX(es_p.FinalScore)  AS decimal(18,2)), 0)             AS BestScore,
                SUM(sts.TotalAttempts)                                              AS TotalQuestionsAttempted,
                CASE WHEN SUM(sts.TotalAttempts) > 0
                     THEN CAST(SUM(sts.CorrectCount) * 100.0 / SUM(sts.TotalAttempts) AS decimal(18,2))
                     ELSE 0 END                                                     AS OverallCorrectPercentage,
                1                                                                   AS StudyStreakDays,
                MAX(sts.LastUpdated)                                                AS LastStudiedAt
            FROM StudentSubTopicPerformances sts
            JOIN Subjects s ON s.Id = sts.SubjectId
            LEFT JOIN (
                SELECT es.Id, es.FinalScore, es.SubjectId
                FROM ExamSessions es
                WHERE es.UserId    = @UserId
                  AND es.Status    = 'Completed'
                  AND es.Mode      = 'MockExam'
                  AND es.SubjectId IS NOT NULL
            ) es_p ON es_p.SubjectId = sts.SubjectId
            WHERE sts.UserId = @UserId
              AND sts.SubjectId NOT IN (
                  SELECT SubjectId FROM StudentSubjectPerformances WHERE UserId = @UserId
              )
            GROUP BY sts.SubjectId, s.NameEnglish

            ORDER BY SubjectName
            """,
            new { UserId = currentUser.UserId });

        return rows.ToList();
    }
}
