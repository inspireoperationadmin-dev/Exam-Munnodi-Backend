using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.Analytics.DTOs;

namespace ScholarFlow.Modules.Analytics.Queries.GetStudentProgressSummary;

public sealed class GetStudentProgressSummaryQueryHandler(
    ISqlConnectionFactory sql,
    ICurrentUser currentUser,
    ISubscriptionsApi subscriptionsApi)
    : IRequestHandler<GetStudentProgressSummaryQuery, StudentProgressSummaryDto>
{
    private const int FocusSubTopicLimit = 8;
    private const int RecentExamLimit = 5;

    public async Task<StudentProgressSummaryDto> Handle(GetStudentProgressSummaryQuery request, CancellationToken ct)
    {
        var subscription = await subscriptionsApi.GetAccessSummaryAsync(currentUser.UserId, ct);

        using var conn = sql.CreateConnection();
        var args = new
        {
            UserId = currentUser.UserId,
            request.SubjectId,
            FocusSubTopicLimit,
            RecentExamLimit
        };

        var subjects = await conn.QueryAsync<StudentProgressSubjectDto>("""
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
            ),
            SubjectQuestionTotals AS (
                SELECT
                    t.SubjectId,
                    COUNT(q.Id) AS TotalQuestions
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
                    SUM(CASE WHEN MasteryScore >= 100 THEN 1 ELSE 0 END) AS MasteredQuestions,
                    SUM(MasteryScore) AS MasteryScoreTotal,
                    MAX(LastSeenAt) AS LastStudiedAt
                FROM StudentQuestionProgresses
                WHERE UserId = @UserId
                GROUP BY SubjectId
            )
            SELECT
                scope.SubjectId,
                CASE
                    WHEN profile.Medium = 2 THEN COALESCE(NULLIF(subject.NameTamil, N''), subject.NameEnglish)
                    WHEN profile.Medium = 1 THEN COALESCE(NULLIF(subject.NameSinhala, N''), subject.NameEnglish)
                    ELSE subject.NameEnglish
                END AS SubjectName,
                CAST(
                    CASE WHEN COALESCE(totals.TotalQuestions, 0) > 0
                        THEN ROUND(CAST(COALESCE(progress.MasteryScoreTotal, 0) AS decimal(18, 4)) / totals.TotalQuestions, 2)
                        ELSE 0
                    END AS decimal(5, 2)
                ) AS MasteryPercentage,
                COALESCE(progress.MasteredQuestions, 0) AS MasteredQuestions,
                COALESCE(totals.TotalQuestions, 0) AS TotalQuestions,
                progress.LastStudiedAt
            FROM SubjectScope scope
            JOIN Subjects subject ON subject.Id = scope.SubjectId
            LEFT JOIN StudentProfiles profile ON profile.UserId = @UserId
            LEFT JOIN SubjectQuestionTotals totals ON totals.SubjectId = scope.SubjectId
            LEFT JOIN ProgressAggregates progress ON progress.SubjectId = scope.SubjectId
            WHERE subject.IsDeleted = 0
              AND (@SubjectId IS NULL OR scope.SubjectId = @SubjectId)
            ORDER BY SubjectName
            """, args);

        if (subscription.ProgressAccessLevel == ProgressAccessLevel.Overall)
        {
            return new StudentProgressSummaryDto(
                subjects.ToList(),
                [],
                [],
                []);
        }

        var topics = await conn.QueryAsync<StudentProgressTopicDto>("""
            WITH SubjectScope AS (
                SELECT DISTINCT sss.SubjectId
                FROM StudentProfiles profile
                JOIN StudentSubjectSelections sss ON sss.StudentProfileId = profile.Id
                WHERE profile.UserId = @UserId
            ),
            TopicQuestionTotals AS (
                SELECT
                    st.TopicId,
                    COUNT(q.Id) AS TotalQuestions
                FROM Questions q
                JOIN Papers p ON p.Id = q.PaperId
                JOIN SubTopics st ON st.Id = q.SubTopicId
                JOIN Topics t ON t.Id = st.TopicId
                WHERE q.IsDeleted = 0
                  AND p.IsDeleted = 0
                  AND p.IsPublic = 1
                  AND st.IsDeleted = 0
                  AND t.IsDeleted = 0
                GROUP BY st.TopicId
            ),
            ProgressAggregates AS (
                SELECT
                    TopicId,
                    SUM(CASE WHEN MasteryScore >= 100 THEN 1 ELSE 0 END) AS MasteredQuestions,
                    SUM(MasteryScore) AS MasteryScoreTotal,
                    MAX(LastSeenAt) AS LastUpdated
                FROM StudentQuestionProgresses
                WHERE UserId = @UserId
                GROUP BY TopicId
            ),
            SubTopicProgress AS (
                SELECT
                    st.TopicId,
                    st.Id AS SubTopicId,
                    CAST(
                        CASE WHEN COUNT(q.Id) > 0
                            THEN ROUND(CAST(COALESCE(SUM(progress.MasteryScore), 0) AS decimal(18, 4)) / COUNT(q.Id), 2)
                            ELSE 0
                        END AS decimal(5, 2)
                    ) AS MasteryPercentage,
                    COUNT(progress.QuestionId) AS ProgressQuestionCount
                FROM SubTopics st
                JOIN Topics t ON t.Id = st.TopicId
                JOIN Questions q ON q.SubTopicId = st.Id AND q.IsDeleted = 0
                JOIN Papers p ON p.Id = q.PaperId AND p.IsDeleted = 0 AND p.IsPublic = 1
                LEFT JOIN StudentQuestionProgresses progress
                    ON progress.QuestionId = q.Id AND progress.UserId = @UserId
                WHERE st.IsDeleted = 0
                  AND t.IsDeleted = 0
                GROUP BY st.TopicId, st.Id
            )
            SELECT
                topic.SubjectId,
                topic.Id AS TopicId,
                CASE
                    WHEN profile.Medium = 2 THEN COALESCE(NULLIF(topic.NameTamil, N''), topic.NameEnglish)
                    WHEN profile.Medium = 1 THEN COALESCE(NULLIF(topic.NameSinhala, N''), topic.NameEnglish)
                    ELSE topic.NameEnglish
                END AS TopicName,
                CAST(
                    CASE WHEN COALESCE(totals.TotalQuestions, 0) > 0
                        THEN ROUND(CAST(COALESCE(progress.MasteryScoreTotal, 0) AS decimal(18, 4)) / totals.TotalQuestions, 2)
                        ELSE 0
                    END AS decimal(5, 2)
                ) AS MasteryPercentage,
                COALESCE(progress.MasteredQuestions, 0) AS MasteredQuestions,
                COALESCE(totals.TotalQuestions, 0) AS TotalQuestions,
                COALESCE(SUM(CASE
                    WHEN subtopicProgress.ProgressQuestionCount > 0
                     AND subtopicProgress.MasteryPercentage < 100 THEN 1
                    ELSE 0
                END), 0) AS NeedsImprovementSubTopicCount,
                progress.LastUpdated
            FROM Topics topic
            JOIN SubjectScope scope ON scope.SubjectId = topic.SubjectId
            LEFT JOIN StudentProfiles profile ON profile.UserId = @UserId
            LEFT JOIN TopicQuestionTotals totals ON totals.TopicId = topic.Id
            LEFT JOIN ProgressAggregates progress ON progress.TopicId = topic.Id
            LEFT JOIN SubTopicProgress subtopicProgress ON subtopicProgress.TopicId = topic.Id
            WHERE topic.IsDeleted = 0
              AND (@SubjectId IS NULL OR topic.SubjectId = @SubjectId)
            GROUP BY
                topic.SubjectId,
                topic.Id,
                topic.NameEnglish,
                topic.NameTamil,
                topic.NameSinhala,
                profile.Medium,
                totals.TotalQuestions,
                progress.MasteredQuestions,
                progress.MasteryScoreTotal,
                progress.LastUpdated
            ORDER BY TopicName
            """, args);

        var focusRows = await conn.QueryAsync<FocusSubTopicRow>("""
            WITH SubjectScope AS (
                SELECT DISTINCT sss.SubjectId
                FROM StudentProfiles profile
                JOIN StudentSubjectSelections sss ON sss.StudentProfileId = profile.Id
                WHERE profile.UserId = @UserId
            ),
            SubTopicProgress AS (
                SELECT
                    topic.SubjectId,
                    subject.NameEnglish AS SubjectNameEnglish,
                    subject.NameTamil AS SubjectNameTamil,
                    subject.NameSinhala AS SubjectNameSinhala,
                    topic.Id AS TopicId,
                    topic.NameEnglish AS TopicNameEnglish,
                    topic.NameTamil AS TopicNameTamil,
                    topic.NameSinhala AS TopicNameSinhala,
                    subtopic.Id AS SubTopicId,
                    subtopic.NameEnglish AS SubTopicNameEnglish,
                    subtopic.NameTamil AS SubTopicNameTamil,
                    subtopic.NameSinhala AS SubTopicNameSinhala,
                    COUNT(q.Id) AS TotalQuestions,
                    COUNT(progress.QuestionId) AS ProgressQuestionCount,
                    CAST(
                        CASE WHEN COUNT(q.Id) > 0
                            THEN ROUND(CAST(COALESCE(SUM(progress.MasteryScore), 0) AS decimal(18, 4)) / COUNT(q.Id), 2)
                            ELSE 0
                        END AS decimal(5, 2)
                    ) AS MasteryPercentage,
                    MAX(progress.LastSeenAt) AS LastUpdated
                FROM SubTopics subtopic
                JOIN Topics topic ON topic.Id = subtopic.TopicId
                JOIN Subjects subject ON subject.Id = topic.SubjectId
                JOIN SubjectScope scope ON scope.SubjectId = topic.SubjectId
                JOIN Questions q ON q.SubTopicId = subtopic.Id AND q.IsDeleted = 0
                JOIN Papers p ON p.Id = q.PaperId AND p.IsDeleted = 0 AND p.IsPublic = 1
                LEFT JOIN StudentQuestionProgresses progress
                    ON progress.QuestionId = q.Id AND progress.UserId = @UserId
                WHERE subtopic.IsDeleted = 0
                  AND topic.IsDeleted = 0
                  AND subject.IsDeleted = 0
                  AND (@SubjectId IS NULL OR topic.SubjectId = @SubjectId)
                GROUP BY
                    topic.SubjectId,
                    subject.NameEnglish,
                    subject.NameTamil,
                    subject.NameSinhala,
                    topic.Id,
                    topic.NameEnglish,
                    topic.NameTamil,
                    topic.NameSinhala,
                    subtopic.Id,
                    subtopic.NameEnglish,
                    subtopic.NameTamil,
                    subtopic.NameSinhala
            )
            SELECT TOP (@FocusSubTopicLimit)
                progress.SubjectId,
                CASE
                    WHEN profile.Medium = 2 THEN COALESCE(NULLIF(progress.SubjectNameTamil, N''), progress.SubjectNameEnglish)
                    WHEN profile.Medium = 1 THEN COALESCE(NULLIF(progress.SubjectNameSinhala, N''), progress.SubjectNameEnglish)
                    ELSE progress.SubjectNameEnglish
                END AS SubjectName,
                progress.TopicId,
                CASE
                    WHEN profile.Medium = 2 THEN COALESCE(NULLIF(progress.TopicNameTamil, N''), progress.TopicNameEnglish)
                    WHEN profile.Medium = 1 THEN COALESCE(NULLIF(progress.TopicNameSinhala, N''), progress.TopicNameEnglish)
                    ELSE progress.TopicNameEnglish
                END AS TopicName,
                progress.SubTopicId,
                CASE
                    WHEN profile.Medium = 2 THEN COALESCE(NULLIF(progress.SubTopicNameTamil, N''), progress.SubTopicNameEnglish)
                    WHEN profile.Medium = 1 THEN COALESCE(NULLIF(progress.SubTopicNameSinhala, N''), progress.SubTopicNameEnglish)
                    ELSE progress.SubTopicNameEnglish
                END AS SubTopicName,
                progress.MasteryPercentage,
                progress.LastUpdated
            FROM SubTopicProgress progress
            LEFT JOIN StudentProfiles profile ON profile.UserId = @UserId
            WHERE progress.ProgressQuestionCount > 0
              AND progress.MasteryPercentage < 100
            ORDER BY progress.MasteryPercentage ASC, progress.LastUpdated DESC
            """, args);

        var recentExamScores = subscription.ProgressAccessLevel == ProgressAccessLevel.Full
            ? await conn.QueryAsync<StudentProgressRecentExamDto>("""
                SELECT TOP (@RecentExamLimit)
                    session.Id AS SessionId,
                    COALESCE(session.SubjectId, paper.SubjectId) AS SubjectId,
                    CASE
                        WHEN profile.Medium = 2 THEN COALESCE(NULLIF(subject.NameTamil, N''), subject.NameEnglish)
                        WHEN profile.Medium = 1 THEN COALESCE(NULLIF(subject.NameSinhala, N''), subject.NameEnglish)
                        ELSE subject.NameEnglish
                    END AS SubjectName,
                    session.Mode,
                    session.EndTime AS CompletedAt,
                    session.FinalScore AS Score
                FROM ExamSessions session
                LEFT JOIN Papers paper ON paper.Id = session.PaperId
                LEFT JOIN Subjects subject ON subject.Id = COALESCE(session.SubjectId, paper.SubjectId)
                LEFT JOIN StudentProfiles profile ON profile.UserId = @UserId
                WHERE session.UserId = @UserId
                  AND session.Status = 'Completed'
                  AND session.Mode IN ('MockExam', 'TopicExam')
                  AND (@SubjectId IS NULL OR COALESCE(session.SubjectId, paper.SubjectId) = @SubjectId)
                ORDER BY session.EndTime DESC
                """, args)
            : [];

        var focusSubTopics = focusRows
            .Select(row => new StudentProgressFocusSubTopicDto(
                row.SubjectId,
                row.SubjectName,
                row.TopicId,
                row.TopicName,
                row.SubTopicId,
                row.SubTopicName,
                "Practice this subtopic next."))
            .ToList();

        return new StudentProgressSummaryDto(
            subjects.ToList(),
            topics.ToList(),
            focusSubTopics,
            recentExamScores.ToList());
    }

    private sealed class FocusSubTopicRow
    {
        public Guid SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public Guid TopicId { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public Guid SubTopicId { get; set; }
        public string SubTopicName { get; set; } = string.Empty;
        public decimal MasteryPercentage { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
