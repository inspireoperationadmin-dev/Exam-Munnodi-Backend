using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Analytics.DTOs.Admin;

namespace ScholarFlow.Modules.Analytics.Queries.Admin.GetAdminStudentProgress;

public sealed class GetAdminStudentProgressQueryHandler(ISqlConnectionFactory sql)
    : IRequestHandler<GetAdminStudentProgressQuery, IReadOnlyList<AdminStudentProgressSummaryDto>>
{
    public async Task<IReadOnlyList<AdminStudentProgressSummaryDto>> Handle(
        GetAdminStudentProgressQuery request,
        CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var status = request.Status switch
        {
            AdminStudentStatusFilter.NotStarted => "Not started",
            AdminStudentStatusFilter.SetupOnly => "Setup only",
            null => null,
            _ => request.Status.ToString()
        };

        var rows = (await conn.QueryAsync<StudentProgressRow>("""
            WITH StudentUsers AS (
                SELECT
                    u.Id,
                    u.Email,
                    u.PhoneNumber AS UserPhoneNumber,
                    sp.FullName,
                    sp.PhoneNumber AS ProfilePhoneNumber,
                    sp.Medium,
                    sp.ExamYear,
                    sp.JoinedAt,
                    stream.NameEnglish AS StreamName
                FROM AspNetUsers u
                JOIN AspNetUserRoles ur ON ur.UserId = u.Id
                JOIN AspNetRoles r ON r.Id = ur.RoleId AND r.Name = N'Student'
                LEFT JOIN StudentProfiles sp ON sp.UserId = u.Id
                LEFT JOIN Streams stream ON stream.Id = sp.AcademicStreamId
            ),
            MockStats AS (
                SELECT
                    UserId,
                    COUNT(*) AS MockExamsCompleted,
                    CAST(ROUND(AVG(CAST(FinalScore AS decimal(18, 4))), 2) AS decimal(5, 2)) AS AverageMockScore,
                    CAST(MAX(FinalScore) AS decimal(5, 2)) AS BestMockScore,
                    MAX(EndTime) AS LastMockAt
                FROM ExamSessions
                WHERE Mode = N'MockExam'
                  AND Status IN (N'Completed', N'TimedOut')
                GROUP BY UserId
            ),
            TopicStats AS (
                SELECT
                    UserId,
                    COUNT(*) AS TopicExamsCompleted,
                    MAX(EndTime) AS LastTopicAt
                FROM ExamSessions
                WHERE Mode = N'TopicExam'
                  AND Status IN (N'Completed', N'TimedOut')
                GROUP BY UserId
            ),
            CurrentWeek AS (
                SELECT
                    UserId,
                    AVG(CAST(FinalScore AS decimal(18, 4))) AS CurrentAverage
                FROM ExamSessions
                WHERE Mode = N'MockExam'
                  AND Status IN (N'Completed', N'TimedOut')
                  AND EndTime >= DATEADD(day, -7, SYSUTCDATETIME())
                GROUP BY UserId
            ),
            PreviousWeek AS (
                SELECT
                    UserId,
                    AVG(CAST(FinalScore AS decimal(18, 4))) AS PreviousAverage
                FROM ExamSessions
                WHERE Mode = N'MockExam'
                  AND Status IN (N'Completed', N'TimedOut')
                  AND EndTime >= DATEADD(day, -14, SYSUTCDATETIME())
                  AND EndTime < DATEADD(day, -7, SYSUTCDATETIME())
                GROUP BY UserId
            ),
            LastActivity AS (
                SELECT UserId, MAX(ActivityDate) AS LastActiveAt
                FROM (
                    SELECT UserId, EndTime AS ActivityDate FROM ExamSessions WHERE EndTime IS NOT NULL
                    UNION ALL
                    SELECT UserId, LastActivityAt AS ActivityDate FROM ExamSessions
                    UNION ALL
                    SELECT UserId, LastStudiedAt AS ActivityDate FROM StudentSubjectPerformances WHERE LastStudiedAt IS NOT NULL
                ) activity
                GROUP BY UserId
            ),
            StudentSubjects AS (
                SELECT
                    sp.UserId,
                    STRING_AGG(s.NameEnglish, ', ') WITHIN GROUP (ORDER BY s.NameEnglish) AS Subjects
                FROM StudentProfiles sp
                JOIN StudentSubjectSelections sss ON sss.StudentProfileId = sp.Id
                JOIN Subjects s ON s.Id = sss.SubjectId
                GROUP BY sp.UserId
            ),
            StudentRows AS (
                SELECT
                    su.Id AS StudentId,
                    COALESCE(NULLIF(su.FullName, ''), su.Email) AS FullName,
                    COALESCE(su.Email, '') AS Email,
                    COALESCE(su.ProfilePhoneNumber, su.UserPhoneNumber) AS PhoneNumber,
                    CAST(su.Medium AS nvarchar(32)) AS Medium,
                    su.StreamName,
                    su.ExamYear,
                    COALESCE(su.JoinedAt, CAST('1900-01-01' AS datetime2)) AS JoinedAt,
                    la.LastActiveAt,
                    COALESCE(ms.MockExamsCompleted, 0) AS MockExamsCompleted,
                    COALESCE(ts.TopicExamsCompleted, 0) AS TopicExamsCompleted,
                    COALESCE(ms.AverageMockScore, 0) AS AverageMockScore,
                    COALESCE(ms.BestMockScore, 0) AS BestMockScore,
                    CAST(ROUND(COALESCE(cw.CurrentAverage, 0) - COALESCE(pw.PreviousAverage, 0), 2) AS decimal(5, 2)) AS WeeklyChangePercentage,
                    CASE
                        WHEN la.LastActiveAt IS NULL THEN N'Not started'
                        WHEN la.LastActiveAt < DATEADD(day, -7, SYSUTCDATETIME()) THEN N'Inactive'
                        WHEN COALESCE(ms.MockExamsCompleted, 0) + COALESCE(ts.TopicExamsCompleted, 0) = 0 THEN N'Setup only'
                        WHEN COALESCE(cw.CurrentAverage, 0) - COALESCE(pw.PreviousAverage, 0) > 3 THEN N'Improving'
                        WHEN COALESCE(cw.CurrentAverage, 0) - COALESCE(pw.PreviousAverage, 0) < -3 THEN N'Decreasing'
                        ELSE N'Stable'
                    END AS Status,
                    COALESCE(ss.Subjects, '') AS SubjectsCsv
                FROM StudentUsers su
                LEFT JOIN MockStats ms ON ms.UserId = su.Id
                LEFT JOIN TopicStats ts ON ts.UserId = su.Id
                LEFT JOIN CurrentWeek cw ON cw.UserId = su.Id
                LEFT JOIN PreviousWeek pw ON pw.UserId = su.Id
                LEFT JOIN LastActivity la ON la.UserId = su.Id
                LEFT JOIN StudentSubjects ss ON ss.UserId = su.Id
            )
            SELECT *
            FROM StudentRows
            WHERE @Status IS NULL
               OR (@Status = N'Active' AND LastActiveAt >= DATEADD(day, -7, SYSUTCDATETIME()))
               OR (@Status <> N'Active' AND Status = @Status)
            ORDER BY LastActiveAt DESC, JoinedAt DESC
            """, new { Status = status })).ToList();

        return rows.Select(row => new AdminStudentProgressSummaryDto(
            row.StudentId,
            row.FullName,
            row.Email,
            row.PhoneNumber,
            row.Medium,
            row.StreamName,
            row.ExamYear,
            row.JoinedAt,
            row.LastActiveAt,
            row.MockExamsCompleted,
            row.TopicExamsCompleted,
            row.AverageMockScore,
            row.BestMockScore,
            row.WeeklyChangePercentage,
            row.Status,
            SplitSubjects(row.SubjectsCsv)))
            .ToList();
    }

    private static IReadOnlyList<string> SplitSubjects(string? csv)
        => string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private sealed class StudentProgressRow
    {
        public Guid StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Medium { get; set; }
        public string? StreamName { get; set; }
        public int? ExamYear { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LastActiveAt { get; set; }
        public int MockExamsCompleted { get; set; }
        public int TopicExamsCompleted { get; set; }
        public decimal AverageMockScore { get; set; }
        public decimal BestMockScore { get; set; }
        public decimal WeeklyChangePercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? SubjectsCsv { get; set; }
    }
}
