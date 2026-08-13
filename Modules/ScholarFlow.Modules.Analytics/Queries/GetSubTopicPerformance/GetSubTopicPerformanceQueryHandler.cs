using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Analytics.DTOs;

namespace ScholarFlow.Modules.Analytics.Queries.GetSubTopicPerformance;

public sealed class GetSubTopicPerformanceQueryHandler(
    ISqlConnectionFactory sql,
    ICurrentUser currentUser)
    : IRequestHandler<GetSubTopicPerformanceQuery, List<SubTopicPerformanceDto>>
{
    public async Task<List<SubTopicPerformanceDto>> Handle(GetSubTopicPerformanceQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var rows = await conn.QueryAsync<SubTopicPerformanceDto>("""
            SELECT
                sstp.TopicId,
                sstp.SubTopicId,
                CASE
                    WHEN sp.Medium = 2 THEN COALESCE(NULLIF(st.NameTamil, N''), st.NameEnglish)
                    WHEN sp.Medium = 1 THEN COALESCE(NULLIF(st.NameSinhala, N''), st.NameEnglish)
                    ELSE st.NameEnglish
                END AS SubTopicName,
                CASE
                    WHEN sp.Medium = 2 THEN COALESCE(NULLIF(t.NameTamil, N''), t.NameEnglish)
                    WHEN sp.Medium = 1 THEN COALESCE(NULLIF(t.NameSinhala, N''), t.NameEnglish)
                    ELSE t.NameEnglish
                END AS TopicName,
                sstp.TotalAttempts,
                sstp.CorrectCount,
                sstp.UniqueQuestionsAttempted,
                sstp.MasteredQuestions,
                sstp.CoveragePercentage,
                sstp.MasteryPercentage,
                sstp.CorrectPercentage,
                sstp.HealthPercentage,
                sstp.LastUpdated
            FROM StudentSubTopicPerformances sstp
            JOIN SubTopics st ON st.Id = sstp.SubTopicId
            JOIN Topics t     ON t.Id  = sstp.TopicId
            LEFT JOIN StudentProfiles sp ON sp.UserId = @UserId
            WHERE sstp.UserId    = @UserId
              AND sstp.SubjectId = @SubjectId
            ORDER BY sstp.CorrectPercentage ASC
            """,
            new { UserId = currentUser.UserId, request.SubjectId });

        return rows.ToList();
    }
}
