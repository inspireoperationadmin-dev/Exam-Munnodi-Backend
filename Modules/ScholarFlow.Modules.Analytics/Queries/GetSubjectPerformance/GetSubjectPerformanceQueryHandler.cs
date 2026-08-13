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
            WITH SubjectQuestionTotals AS (
                SELECT
                    t.SubjectId,
                    COUNT(q.Id) AS TotalQuestionsInSubject
                FROM Questions q
                JOIN SubTopics st ON st.Id = q.SubTopicId
                JOIN Topics t ON t.Id = st.TopicId
                WHERE q.IsDeleted = 0
                  AND st.IsDeleted = 0
                  AND t.IsDeleted = 0
                GROUP BY t.SubjectId
            ),
            AnsweredMockResponses AS (
                SELECT
                    t.SubjectId,
                    ur.QuestionId,
                    ur.IsCorrect,
                    ur.OrderIndex,
                    es.StartTime,
                    es.EndTime
                FROM UserResponses ur
                JOIN ExamSessions es ON es.Id = ur.SessionId
                JOIN Questions q ON q.Id = ur.QuestionId
                JOIN SubTopics st ON st.Id = q.SubTopicId
                JOIN Topics t ON t.Id = st.TopicId
                WHERE es.UserId = @UserId
                  AND es.Mode = N'MockExam'
                  AND es.Status IN (N'Completed', N'TimedOut')
                  AND ur.SelectedOptionId IS NOT NULL
                  AND q.IsDeleted = 0
                  AND st.IsDeleted = 0
                  AND t.IsDeleted = 0
            ),
            AnsweredAggregates AS (
                SELECT
                    SubjectId,
                    COUNT(DISTINCT QuestionId) AS UniqueQuestionsAttempted,
                    COUNT(*) AS TotalQuestionsAttempted,
                    SUM(CASE WHEN IsCorrect = 1 THEN 1 ELSE 0 END) AS CorrectCount
                FROM AnsweredMockResponses
                GROUP BY SubjectId
            ),
            LatestAnswered AS (
                SELECT
                    SubjectId,
                    QuestionId,
                    IsCorrect,
                    ROW_NUMBER() OVER (
                        PARTITION BY SubjectId, QuestionId
                        ORDER BY COALESCE(EndTime, StartTime) DESC, OrderIndex DESC
                    ) AS RowNumber
                FROM AnsweredMockResponses
            ),
            MasteryAggregates AS (
                SELECT
                    SubjectId,
                    SUM(CASE WHEN IsCorrect = 1 THEN 1 ELSE 0 END) AS MasteredQuestions
                FROM LatestAnswered
                WHERE RowNumber = 1
                GROUP BY SubjectId
            )
            SELECT
                ssp.SubjectId,
                CASE
                    WHEN sp.Medium = 2 THEN COALESCE(NULLIF(s.NameTamil, N''), s.NameEnglish)
                    WHEN sp.Medium = 1 THEN COALESCE(NULLIF(s.NameSinhala, N''), s.NameEnglish)
                    ELSE s.NameEnglish
                END AS SubjectName,
                COALESCE(sqt.TotalQuestionsInSubject, 0) AS TotalQuestionsInSubject,
                COALESCE(aa.UniqueQuestionsAttempted, 0) AS UniqueQuestionsAttempted,
                COALESCE(ma.MasteredQuestions, 0) AS MasteredQuestions,
                ssp.TotalExams,
                ssp.AverageExamScore AS AverageScore,
                ssp.BestScore,
                COALESCE(aa.TotalQuestionsAttempted, ssp.TotalQuestionsAttempted) AS TotalQuestionsAttempted,
                CAST(
                    CASE WHEN COALESCE(aa.TotalQuestionsAttempted, 0) > 0
                        THEN ROUND(CAST(COALESCE(aa.CorrectCount, 0) AS decimal(18, 4)) / aa.TotalQuestionsAttempted * 100, 2)
                        ELSE ssp.OverallCorrectPercentage
                    END AS decimal(5, 2)
                ) AS OverallCorrectPercentage,
                CAST(
                    CASE WHEN COALESCE(sqt.TotalQuestionsInSubject, 0) > 0
                        THEN ROUND(CAST(COALESCE(aa.UniqueQuestionsAttempted, 0) AS decimal(18, 4)) / sqt.TotalQuestionsInSubject * 100, 2)
                        ELSE 0
                    END AS decimal(5, 2)
                ) AS CoveragePercentage,
                CAST(
                    CASE WHEN COALESCE(sqt.TotalQuestionsInSubject, 0) > 0
                        THEN ROUND(CAST(COALESCE(ma.MasteredQuestions, 0) AS decimal(18, 4)) / sqt.TotalQuestionsInSubject * 100, 2)
                        ELSE 0
                    END AS decimal(5, 2)
                ) AS MasteryPercentage,
                CAST(
                    CASE WHEN COALESCE(aa.TotalQuestionsAttempted, 0) > 0
                        THEN ROUND(CAST(COALESCE(aa.CorrectCount, 0) AS decimal(18, 4)) / aa.TotalQuestionsAttempted * 100, 2)
                        ELSE 0
                    END AS decimal(5, 2)
                ) AS AccuracyPercentage,
                CAST(
                    (
                        0.35 * CASE WHEN COALESCE(sqt.TotalQuestionsInSubject, 0) > 0
                            THEN ROUND(CAST(COALESCE(aa.UniqueQuestionsAttempted, 0) AS decimal(18, 4)) / sqt.TotalQuestionsInSubject * 100, 2)
                            ELSE 0
                        END
                    ) + (
                        0.45 * CASE WHEN COALESCE(sqt.TotalQuestionsInSubject, 0) > 0
                            THEN ROUND(CAST(COALESCE(ma.MasteredQuestions, 0) AS decimal(18, 4)) / sqt.TotalQuestionsInSubject * 100, 2)
                            ELSE 0
                        END
                    ) + (
                        0.20 * CASE WHEN COALESCE(aa.TotalQuestionsAttempted, 0) > 0
                            THEN ROUND(CAST(COALESCE(aa.CorrectCount, 0) AS decimal(18, 4)) / aa.TotalQuestionsAttempted * 100, 2)
                            ELSE 0
                        END
                    )
                    AS decimal(5, 2)
                ) AS ReadinessPercentage,
                ssp.StudyStreakDays,
                ssp.LastStudiedAt
            FROM StudentSubjectPerformances ssp
            JOIN Subjects s ON s.Id = ssp.SubjectId
            LEFT JOIN StudentProfiles sp ON sp.UserId = @UserId
            LEFT JOIN SubjectQuestionTotals sqt ON sqt.SubjectId = ssp.SubjectId
            LEFT JOIN AnsweredAggregates aa ON aa.SubjectId = ssp.SubjectId
            LEFT JOIN MasteryAggregates ma ON ma.SubjectId = ssp.SubjectId
            WHERE ssp.UserId = @UserId
            ORDER BY SubjectName
            """,
            new { UserId = currentUser.UserId });

        return rows.ToList();
    }
}
