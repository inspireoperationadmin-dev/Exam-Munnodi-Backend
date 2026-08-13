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
                st.NameEnglish AS SubTopicName,
                t.NameEnglish  AS TopicName,
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
            WHERE sstp.UserId    = @UserId
              AND sstp.SubjectId = @SubjectId
            ORDER BY sstp.CorrectPercentage ASC
            """,
            new { UserId = currentUser.UserId, request.SubjectId });

        return rows.ToList();
    }
}
