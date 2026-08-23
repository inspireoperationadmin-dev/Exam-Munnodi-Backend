using Dapper;
using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Analytics.DTOs;

namespace ScholarFlow.Modules.Analytics.Queries.GetTopicPerformance;

public sealed class GetTopicPerformanceQueryHandler(
    ISqlConnectionFactory sql,
    ICurrentUser currentUser,
    ISubscriptionsApi subscriptionsApi)
    : IRequestHandler<GetTopicPerformanceQuery, List<TopicPerformanceDto>>
{
    public async Task<List<TopicPerformanceDto>> Handle(GetTopicPerformanceQuery request, CancellationToken ct)
    {
        await subscriptionsApi.EnsureProgressAccessAsync(
            currentUser.UserId,
            ProgressAccessLevel.Detailed,
            ct);

        using var conn = sql.CreateConnection();

        var rows = await conn.QueryAsync<TopicPerformanceDto>("""
            WITH TopicQuestionTotals AS (
                SELECT
                    st.TopicId,
                    COUNT(q.Id) AS TotalQuestionsInTopic
                FROM Questions q
                JOIN Papers p ON p.Id = q.PaperId
                JOIN SubTopics st ON st.Id = q.SubTopicId
                JOIN Topics t ON t.Id = st.TopicId
                WHERE t.SubjectId = @SubjectId
                  AND q.IsDeleted = 0
                  AND p.IsDeleted = 0
                  AND p.IsPublic = 1
                  AND st.IsDeleted = 0
                  AND t.IsDeleted = 0
                GROUP BY st.TopicId
            )
            SELECT
                p.TopicId,
                CASE
                    WHEN sp.Medium = 2 THEN COALESCE(NULLIF(t.NameTamil, N''), t.NameEnglish)
                    WHEN sp.Medium = 1 THEN COALESCE(NULLIF(t.NameSinhala, N''), t.NameEnglish)
                    ELSE t.NameEnglish
                END AS TopicName,
                COALESCE(tqt.TotalQuestionsInTopic, 0) AS TotalQuestionsInTopic,
                COUNT(p.QuestionId) AS UniqueQuestionsAttempted,
                SUM(CASE WHEN p.MasteryScore >= 100 THEN 1 ELSE 0 END) AS MasteredQuestions,
                SUM(p.TimesAttempted) AS TotalAttempts,
                SUM(p.CorrectCount) AS CorrectCount,
                CAST(
                    CASE WHEN COALESCE(tqt.TotalQuestionsInTopic, 0) > 0
                        THEN ROUND(CAST(COUNT(p.QuestionId) AS decimal(18, 4)) / tqt.TotalQuestionsInTopic * 100, 2)
                        ELSE 0
                    END AS decimal(5, 2)
                ) AS CoveragePercentage,
                CAST(
                    CASE WHEN COALESCE(tqt.TotalQuestionsInTopic, 0) > 0
                        THEN ROUND(CAST(SUM(p.MasteryScore) AS decimal(18, 4)) / tqt.TotalQuestionsInTopic, 2)
                        ELSE 0
                    END AS decimal(5, 2)
                ) AS MasteryPercentage,
                CAST(
                    CASE WHEN SUM(p.TimesAttempted) > 0
                        THEN ROUND(CAST(SUM(p.CorrectCount) AS decimal(18, 4)) / SUM(p.TimesAttempted) * 100, 2)
                        ELSE 0
                    END AS decimal(5, 2)
                ) AS AccuracyPercentage,
                CAST(
                    CASE WHEN COUNT(p.QuestionId) > 0
                        THEN ROUND(CAST(SUM(CASE WHEN p.MasteryScore >= 100 THEN 1 ELSE 0 END) AS decimal(18, 4)) / COUNT(p.QuestionId) * 100, 2)
                        ELSE 0
                    END AS decimal(5, 2)
                ) AS HealthPercentage,
                MAX(p.LastSeenAt) AS LastUpdated
            FROM StudentQuestionProgresses p
            JOIN Topics t ON t.Id = p.TopicId
            LEFT JOIN StudentProfiles sp ON sp.UserId = @UserId
            LEFT JOIN TopicQuestionTotals tqt ON tqt.TopicId = p.TopicId
            WHERE p.UserId = @UserId
              AND p.SubjectId = @SubjectId
              AND t.IsDeleted = 0
            GROUP BY
                p.TopicId,
                t.NameEnglish,
                t.NameTamil,
                t.NameSinhala,
                sp.Medium,
                tqt.TotalQuestionsInTopic
            ORDER BY TopicName
            """,
            new { UserId = currentUser.UserId, request.SubjectId });

        return rows.ToList();
    }
}
