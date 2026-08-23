using Dapper;
using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Analytics.DTOs;

namespace ScholarFlow.Modules.Analytics.Queries.GetSubjectPerformance;

public sealed class GetSubjectPerformanceQueryHandler(
    ISqlConnectionFactory sql,
    ICurrentUser currentUser,
    ISubscriptionsApi subscriptionsApi)
    : IRequestHandler<GetSubjectPerformanceQuery, List<SubjectPerformanceDto>>
{
    public async Task<List<SubjectPerformanceDto>> Handle(GetSubjectPerformanceQuery request, CancellationToken ct)
    {
        await subscriptionsApi.EnsureProgressAccessAsync(
            currentUser.UserId,
            ProgressAccessLevel.Overall,
            ct);

        using var conn = sql.CreateConnection();

        var rows = await conn.QueryAsync<SubjectPerformanceDto>("""
            WITH SelectedSubjects AS (
                SELECT DISTINCT sss.SubjectId
                FROM StudentProfiles profile
                JOIN StudentSubjectSelections sss ON sss.StudentProfileId = profile.Id
                WHERE profile.UserId = @UserId
            ),
            ProgressSubjects AS (
                SELECT DISTINCT SubjectId
                FROM StudentQuestionProgresses
                WHERE UserId = @UserId
            ),
            SubjectScope AS (
                SELECT SubjectId FROM SelectedSubjects
                UNION
                SELECT SubjectId FROM ProgressSubjects
                UNION
                SELECT SubjectId FROM StudentSubjectPerformances WHERE UserId = @UserId
            ),
            SubjectQuestionTotals AS (
                SELECT
                    t.SubjectId,
                    COUNT(q.Id) AS TotalQuestionsInSubject
                FROM Questions q
                JOIN Papers p ON p.Id = q.PaperId
                JOIN SubTopics st ON st.Id = q.SubTopicId
                JOIN Topics t ON t.Id = st.TopicId
                WHERE q.IsDeleted = 0
                  AND p.IsDeleted = 0
                  AND p.IsPublic = 1
                  AND st.IsDeleted = 0
                  AND t.IsDeleted = 0
                GROUP BY t.SubjectId
            ),
            ProgressAggregates AS (
                SELECT
                    SubjectId,
                    COUNT(QuestionId) AS UniqueQuestionsAttempted,
                    SUM(CASE WHEN MasteryScore >= 100 THEN 1 ELSE 0 END) AS MasteredQuestions,
                    SUM(TimesAttempted) AS TotalQuestionsAttempted,
                    SUM(CorrectCount) AS CorrectCount,
                    SUM(MasteryScore) AS MasteryScoreTotal,
                    MAX(LastSeenAt) AS LastSeenAt
                FROM StudentQuestionProgresses
                WHERE UserId = @UserId
                GROUP BY SubjectId
            )
            SELECT
                scope.SubjectId,
                CASE
                    WHEN sp.Medium = 2 THEN COALESCE(NULLIF(s.NameTamil, N''), s.NameEnglish)
                    WHEN sp.Medium = 1 THEN COALESCE(NULLIF(s.NameSinhala, N''), s.NameEnglish)
                    ELSE s.NameEnglish
                END AS SubjectName,
                COALESCE(sqt.TotalQuestionsInSubject, 0) AS TotalQuestionsInSubject,
                COALESCE(pa.UniqueQuestionsAttempted, 0) AS UniqueQuestionsAttempted,
                COALESCE(pa.MasteredQuestions, 0) AS MasteredQuestions,
                COALESCE(ssp.TotalExams, 0) AS TotalExams,
                COALESCE(ssp.AverageExamScore, 0) AS AverageScore,
                COALESCE(ssp.BestScore, 0) AS BestScore,
                COALESCE(pa.TotalQuestionsAttempted, 0) AS TotalQuestionsAttempted,
                CAST(
                    CASE WHEN COALESCE(pa.TotalQuestionsAttempted, 0) > 0
                        THEN ROUND(CAST(COALESCE(pa.CorrectCount, 0) AS decimal(18, 4)) / pa.TotalQuestionsAttempted * 100, 2)
                        ELSE COALESCE(ssp.OverallCorrectPercentage, 0)
                    END AS decimal(5, 2)
                ) AS OverallCorrectPercentage,
                CAST(
                    CASE WHEN COALESCE(sqt.TotalQuestionsInSubject, 0) > 0
                        THEN ROUND(CAST(COALESCE(pa.UniqueQuestionsAttempted, 0) AS decimal(18, 4)) / sqt.TotalQuestionsInSubject * 100, 2)
                        ELSE 0
                    END AS decimal(5, 2)
                ) AS CoveragePercentage,
                CAST(
                    CASE WHEN COALESCE(sqt.TotalQuestionsInSubject, 0) > 0
                        THEN ROUND(CAST(COALESCE(pa.MasteryScoreTotal, 0) AS decimal(18, 4)) / sqt.TotalQuestionsInSubject, 2)
                        ELSE 0
                    END AS decimal(5, 2)
                ) AS MasteryPercentage,
                CAST(
                    CASE WHEN COALESCE(pa.TotalQuestionsAttempted, 0) > 0
                        THEN ROUND(CAST(COALESCE(pa.CorrectCount, 0) AS decimal(18, 4)) / pa.TotalQuestionsAttempted * 100, 2)
                        ELSE 0
                    END AS decimal(5, 2)
                ) AS AccuracyPercentage,
                CAST(
                    (
                        0.35 * CASE WHEN COALESCE(sqt.TotalQuestionsInSubject, 0) > 0
                            THEN ROUND(CAST(COALESCE(pa.UniqueQuestionsAttempted, 0) AS decimal(18, 4)) / sqt.TotalQuestionsInSubject * 100, 2)
                            ELSE 0
                        END
                    ) + (
                        0.45 * CASE WHEN COALESCE(sqt.TotalQuestionsInSubject, 0) > 0
                            THEN ROUND(CAST(COALESCE(pa.MasteryScoreTotal, 0) AS decimal(18, 4)) / sqt.TotalQuestionsInSubject, 2)
                            ELSE 0
                        END
                    ) + (
                        0.20 * CASE WHEN COALESCE(pa.TotalQuestionsAttempted, 0) > 0
                            THEN ROUND(CAST(COALESCE(pa.CorrectCount, 0) AS decimal(18, 4)) / pa.TotalQuestionsAttempted * 100, 2)
                            ELSE 0
                        END
                    )
                    AS decimal(5, 2)
                ) AS ReadinessPercentage,
                COALESCE(ssp.StudyStreakDays, 0) AS StudyStreakDays,
                COALESCE(ssp.LastStudiedAt, pa.LastSeenAt) AS LastStudiedAt
            FROM SubjectScope scope
            JOIN Subjects s ON s.Id = scope.SubjectId
            LEFT JOIN StudentProfiles sp ON sp.UserId = @UserId
            LEFT JOIN StudentSubjectPerformances ssp ON ssp.UserId = @UserId AND ssp.SubjectId = scope.SubjectId
            LEFT JOIN SubjectQuestionTotals sqt ON sqt.SubjectId = scope.SubjectId
            LEFT JOIN ProgressAggregates pa ON pa.SubjectId = scope.SubjectId
            WHERE s.IsDeleted = 0
            ORDER BY SubjectName
            """,
            new { UserId = currentUser.UserId });

        return rows.ToList();
    }
}
