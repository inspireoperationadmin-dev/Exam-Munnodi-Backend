using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Queries.GetActiveSession;

public sealed class GetActiveSessionQueryHandler(
    ICurrentUser currentUser,
    ISqlConnectionFactory sql)
    : IRequestHandler<GetActiveSessionQuery, ActiveSessionDto?>
{
    public async Task<ActiveSessionDto?> Handle(GetActiveSessionQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();
        var now = DateTime.UtcNow;
        var practiceCutoff = now.AddHours(-24);

        var session = await conn.QueryFirstOrDefaultAsync<ActiveSessionRow>("""
            SELECT TOP 1
                es.Id AS SessionId,
                es.Mode,
                es.PaperId,
                es.SubjectId,
                COALESCE(
                    p.Title,
                    CASE
                        WHEN sp.Medium = 2 THEN COALESCE(NULLIF(s.NameTamil, N''), s.NameEnglish)
                        WHEN sp.Medium = 1 THEN COALESCE(NULLIF(s.NameSinhala, N''), s.NameEnglish)
                        ELSE s.NameEnglish
                    END
                ) AS Title,
                es.StartTime,
                es.LastActivityAt,
                es.ExpiresAt,
                SUM(CASE WHEN ur.SelectedOptionId IS NOT NULL THEN 1 ELSE 0 END) AS AnsweredCount,
                COUNT(ur.Id) AS TotalQuestions
            FROM ExamSessions es
            LEFT JOIN Papers p ON p.Id = es.PaperId
            LEFT JOIN Subjects s ON s.Id = es.SubjectId
            LEFT JOIN StudentProfiles sp ON sp.UserId = @UserId
            LEFT JOIN UserResponses ur ON ur.SessionId = es.Id
            WHERE es.UserId = @UserId
              AND es.Status = 'InProgress'
              AND (@SubjectId IS NULL OR es.SubjectId = @SubjectId)
              AND (
                    (es.ExpiresAt IS NOT NULL AND es.ExpiresAt > @Now)
                    OR (es.Mode IN ('PaperPractice', 'TopicPractice') AND es.LastActivityAt >= @PracticeCutoff)
                  )
            GROUP BY es.Id, es.Mode, es.PaperId, es.SubjectId, p.Title,
                     s.NameEnglish, s.NameTamil, s.NameSinhala, sp.Medium,
                     es.StartTime, es.LastActivityAt, es.ExpiresAt
            ORDER BY es.LastActivityAt DESC, es.StartTime DESC
            """, new
        {
            currentUser.UserId,
            request.SubjectId,
            Now = now,
            PracticeCutoff = practiceCutoff
        });

        if (session is null)
        {
            return null;
        }

        var remainingSeconds = session.ExpiresAt.HasValue
            ? Math.Max(0, (int)Math.Ceiling((session.ExpiresAt.Value - now).TotalSeconds))
            : (int?)null;

        return new ActiveSessionDto(
            session.SessionId,
            session.Mode,
            session.PaperId,
            session.SubjectId,
            session.Title,
            session.StartTime,
            session.LastActivityAt,
            session.ExpiresAt,
            now,
            session.AnsweredCount,
            session.TotalQuestions,
            remainingSeconds);
    }

    private sealed class ActiveSessionRow
    {
        public Guid SessionId { get; set; }
        public string Mode { get; set; } = string.Empty;
        public Guid? PaperId { get; set; }
        public Guid? SubjectId { get; set; }
        public string? Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastActivityAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int AnsweredCount { get; set; }
        public int TotalQuestions { get; set; }
    }
}
