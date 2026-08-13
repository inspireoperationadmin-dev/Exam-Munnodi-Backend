using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Analytics.DTOs.Admin;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Analytics.Queries.Admin.GetAdminStudentProgressDetail;

public sealed class GetAdminStudentProgressDetailQueryHandler(ISqlConnectionFactory sql)
    : IRequestHandler<GetAdminStudentProgressDetailQuery, AdminStudentProgressDetailDto>
{
    public async Task<AdminStudentProgressDetailDto> Handle(
        GetAdminStudentProgressDetailQuery request,
        CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var profile = await conn.QuerySingleOrDefaultAsync<StudentProfileRow>("""
            SELECT
                u.Id AS StudentId,
                COALESCE(NULLIF(sp.FullName, ''), u.Email) AS FullName,
                COALESCE(u.Email, '') AS Email,
                COALESCE(sp.PhoneNumber, u.PhoneNumber) AS PhoneNumber,
                CAST(sp.Medium AS nvarchar(32)) AS Medium,
                stream.NameEnglish AS StreamName,
                sp.ExamYear,
                COALESCE(sp.JoinedAt, CAST('1900-01-01' AS datetime2)) AS JoinedAt
            FROM AspNetUsers u
            JOIN AspNetUserRoles ur ON ur.UserId = u.Id
            JOIN AspNetRoles r ON r.Id = ur.RoleId AND r.Name = N'Student'
            LEFT JOIN StudentProfiles sp ON sp.UserId = u.Id
            LEFT JOIN Streams stream ON stream.Id = sp.AcademicStreamId
            WHERE u.Id = @StudentId
            """, new { request.StudentId });

        if (profile is null)
            throw new NotFoundException("Student not found.");

        var subjects = (await conn.QueryAsync<string>("""
            SELECT s.NameEnglish
            FROM StudentProfiles sp
            JOIN StudentSubjectSelections sss ON sss.StudentProfileId = sp.Id
            JOIN Subjects s ON s.Id = sss.SubjectId
            WHERE sp.UserId = @StudentId
            ORDER BY s.NameEnglish
            """, new { request.StudentId })).ToList();

        var subjectProgress = (await conn.QueryAsync<AdminStudentSubjectProgressDto>("""
            WITH SelectedSubjects AS (
                SELECT
                    s.Id AS SubjectId,
                    s.NameEnglish AS SubjectName
                FROM StudentProfiles sp
                JOIN StudentSubjectSelections sss ON sss.StudentProfileId = sp.Id
                JOIN Subjects s ON s.Id = sss.SubjectId
                WHERE sp.UserId = @StudentId
            ),
            SubjectQuestionTotals AS (
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
                WHERE es.UserId = @StudentId
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
            ),
            MockSessionStats AS (
                SELECT
                    SubjectId,
                    COUNT(*) AS MockExamsCompleted,
                    CAST(ROUND(AVG(CAST(FinalScore AS decimal(18, 4))), 2) AS decimal(5, 2)) AS AverageMockScore,
                    CAST(MAX(FinalScore) AS decimal(5, 2)) AS BestMockScore,
                    MAX(EndTime) AS LastStudiedAt
                FROM ExamSessions
                WHERE UserId = @StudentId
                  AND Mode = N'MockExam'
                  AND Status IN (N'Completed', N'TimedOut')
                GROUP BY SubjectId
            ),
            CurrentWeek AS (
                SELECT
                    SubjectId,
                    AVG(CAST(FinalScore AS decimal(18, 4))) AS CurrentAverage
                FROM ExamSessions
                WHERE UserId = @StudentId
                  AND Mode = N'MockExam'
                  AND Status IN (N'Completed', N'TimedOut')
                  AND EndTime >= DATEADD(day, -7, SYSUTCDATETIME())
                GROUP BY SubjectId
            ),
            PreviousWeek AS (
                SELECT
                    SubjectId,
                    AVG(CAST(FinalScore AS decimal(18, 4))) AS PreviousAverage
                FROM ExamSessions
                WHERE UserId = @StudentId
                  AND Mode = N'MockExam'
                  AND Status IN (N'Completed', N'TimedOut')
                  AND EndTime >= DATEADD(day, -14, SYSUTCDATETIME())
                  AND EndTime < DATEADD(day, -7, SYSUTCDATETIME())
                GROUP BY SubjectId
            )
            SELECT
                ss.SubjectId,
                ss.SubjectName,
                COALESCE(sqt.TotalQuestionsInSubject, 0) AS TotalQuestionsInSubject,
                COALESCE(aa.UniqueQuestionsAttempted, 0) AS UniqueQuestionsAttempted,
                COALESCE(ma.MasteredQuestions, 0) AS MasteredQuestions,
                COALESCE(mss.MockExamsCompleted, 0) AS MockExamsCompleted,
                COALESCE(mss.AverageMockScore, 0) AS AverageMockScore,
                COALESCE(mss.BestMockScore, 0) AS BestMockScore,
                CAST(CASE WHEN COALESCE(sqt.TotalQuestionsInSubject, 0) > 0
                    THEN ROUND(CAST(COALESCE(aa.UniqueQuestionsAttempted, 0) AS decimal(18, 4)) / sqt.TotalQuestionsInSubject * 100, 2)
                    ELSE 0 END AS decimal(5, 2)) AS CoveragePercentage,
                CAST(CASE WHEN COALESCE(sqt.TotalQuestionsInSubject, 0) > 0
                    THEN ROUND(CAST(COALESCE(ma.MasteredQuestions, 0) AS decimal(18, 4)) / sqt.TotalQuestionsInSubject * 100, 2)
                    ELSE 0 END AS decimal(5, 2)) AS MasteryPercentage,
                CAST(CASE WHEN COALESCE(aa.TotalQuestionsAttempted, 0) > 0
                    THEN ROUND(CAST(COALESCE(aa.CorrectCount, 0) AS decimal(18, 4)) / aa.TotalQuestionsAttempted * 100, 2)
                    ELSE 0 END AS decimal(5, 2)) AS AccuracyPercentage,
                CAST((
                    0.35 * CASE WHEN COALESCE(sqt.TotalQuestionsInSubject, 0) > 0
                        THEN ROUND(CAST(COALESCE(aa.UniqueQuestionsAttempted, 0) AS decimal(18, 4)) / sqt.TotalQuestionsInSubject * 100, 2)
                        ELSE 0 END
                    + 0.45 * CASE WHEN COALESCE(sqt.TotalQuestionsInSubject, 0) > 0
                        THEN ROUND(CAST(COALESCE(ma.MasteredQuestions, 0) AS decimal(18, 4)) / sqt.TotalQuestionsInSubject * 100, 2)
                        ELSE 0 END
                    + 0.20 * CASE WHEN COALESCE(aa.TotalQuestionsAttempted, 0) > 0
                        THEN ROUND(CAST(COALESCE(aa.CorrectCount, 0) AS decimal(18, 4)) / aa.TotalQuestionsAttempted * 100, 2)
                        ELSE 0 END
                ) AS decimal(5, 2)) AS ReadinessPercentage,
                CAST(ROUND(COALESCE(cw.CurrentAverage, 0) - COALESCE(pw.PreviousAverage, 0), 2) AS decimal(5, 2)) AS WeeklyChangePercentage,
                mss.LastStudiedAt
            FROM SelectedSubjects ss
            LEFT JOIN SubjectQuestionTotals sqt ON sqt.SubjectId = ss.SubjectId
            LEFT JOIN AnsweredAggregates aa ON aa.SubjectId = ss.SubjectId
            LEFT JOIN MasteryAggregates ma ON ma.SubjectId = ss.SubjectId
            LEFT JOIN MockSessionStats mss ON mss.SubjectId = ss.SubjectId
            LEFT JOIN CurrentWeek cw ON cw.SubjectId = ss.SubjectId
            LEFT JOIN PreviousWeek pw ON pw.SubjectId = ss.SubjectId
            ORDER BY ss.SubjectName
            """, new { request.StudentId })).ToList();

        var focusSubTopics = (await conn.QueryAsync<AdminStudentFocusSubTopicDto>("""
            SELECT TOP(8)
                sstp.SubjectId,
                s.NameEnglish AS SubjectName,
                sstp.TopicId,
                t.NameEnglish AS TopicName,
                sstp.SubTopicId,
                st.NameEnglish AS SubTopicName,
                sstp.MasteryPercentage,
                sstp.CorrectPercentage AS AccuracyPercentage,
                sstp.HealthPercentage,
                sstp.TotalAttempts,
                sstp.LastUpdated
            FROM StudentSubTopicPerformances sstp
            JOIN Subjects s ON s.Id = sstp.SubjectId
            JOIN Topics t ON t.Id = sstp.TopicId
            JOIN SubTopics st ON st.Id = sstp.SubTopicId
            WHERE sstp.UserId = @StudentId
              AND sstp.TotalAttempts > 0
            ORDER BY sstp.MasteryPercentage ASC, sstp.HealthPercentage ASC, sstp.LastUpdated DESC
            """, new { request.StudentId })).ToList();

        var weeklyTrend = (await conn.QueryAsync<AdminStudentWeeklyTrendDto>("""
            SELECT
                DATEADD(day, -DATEDIFF(day, 0, CAST(EndTime AS date)) % 7, CAST(EndTime AS date)) AS WeekStartDate,
                COUNT(*) AS ExamCount,
                CAST(ROUND(AVG(CAST(FinalScore AS decimal(18, 4))), 2) AS decimal(5, 2)) AS AverageScore
            FROM ExamSessions
            WHERE UserId = @StudentId
              AND Mode = N'MockExam'
              AND Status IN (N'Completed', N'TimedOut')
              AND EndTime >= DATEADD(day, -56, SYSUTCDATETIME())
            GROUP BY DATEADD(day, -DATEDIFF(day, 0, CAST(EndTime AS date)) % 7, CAST(EndTime AS date))
            ORDER BY WeekStartDate ASC
            """, new { request.StudentId })).ToList();

        var recentExams = (await conn.QueryAsync<AdminStudentRecentExamDto>("""
            SELECT TOP(12)
                es.Id AS SessionId,
                es.SubjectId,
                s.NameEnglish AS SubjectName,
                es.Mode,
                es.Status,
                es.EndTime AS CompletedAt,
                es.FinalScore,
                es.ObtainedMarks,
                es.TotalMarks,
                SUM(CASE WHEN ur.SelectedOptionId IS NOT NULL THEN 1 ELSE 0 END) AS AnsweredCount,
                COUNT(ur.Id) AS TotalQuestions
            FROM ExamSessions es
            LEFT JOIN Subjects s ON s.Id = es.SubjectId
            LEFT JOIN UserResponses ur ON ur.SessionId = es.Id
            WHERE es.UserId = @StudentId
              AND es.Status IN (N'Completed', N'TimedOut')
              AND es.Mode IN (N'MockExam', N'TopicExam', N'PaperExam')
            GROUP BY
                es.Id, es.SubjectId, s.NameEnglish, es.Mode, es.Status,
                es.EndTime, es.FinalScore, es.ObtainedMarks, es.TotalMarks
            ORDER BY es.EndTime DESC
            """, new { request.StudentId })).ToList();

        var mockExamsCompleted = recentExams.Count(e => e.Mode == "MockExam");
        var topicExamsCompleted = recentExams.Count(e => e.Mode == "TopicExam");
        var averageMockScore = subjectProgress.Count == 0 ? 0 : subjectProgress.Average(s => s.AverageMockScore);
        var bestMockScore = subjectProgress.Count == 0 ? 0 : subjectProgress.Max(s => s.BestMockScore);
        var weeklyChange = subjectProgress.Count == 0 ? 0 : subjectProgress.Average(s => s.WeeklyChangePercentage);
        var lastActiveAt = recentExams
            .Where(e => e.CompletedAt.HasValue)
            .Select(e => e.CompletedAt)
            .DefaultIfEmpty()
            .Max();

        return new AdminStudentProgressDetailDto(
            profile.StudentId,
            profile.FullName,
            profile.Email,
            profile.PhoneNumber,
            profile.Medium,
            profile.StreamName,
            profile.ExamYear,
            profile.JoinedAt,
            lastActiveAt,
            mockExamsCompleted,
            topicExamsCompleted,
            Math.Round(averageMockScore, 2),
            bestMockScore,
            Math.Round(weeklyChange, 2),
            subjects,
            subjectProgress,
            focusSubTopics,
            weeklyTrend,
            recentExams);
    }

    private sealed class StudentProfileRow
    {
        public Guid StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Medium { get; set; }
        public string? StreamName { get; set; }
        public int? ExamYear { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
