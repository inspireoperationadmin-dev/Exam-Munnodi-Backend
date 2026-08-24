using Dapper;
using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Examination.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Examination.Queries.GetSessionDetail;

public sealed class GetSessionDetailQueryHandler(
    ISqlConnectionFactory sql,
    ICurrentUser currentUser,
    ISubscriptionsApi subscriptionsApi)
    : IRequestHandler<GetSessionDetailQuery, SessionDetailDto>
{
    private static readonly TimeSpan ReviewRetentionPeriod = TimeSpan.FromDays(3);

    public async Task<SessionDetailDto> Handle(GetSessionDetailQuery request, CancellationToken ct)
    {
        await subscriptionsApi.EnsureActiveAccessAsync(currentUser.UserId, ct);

        using var conn = sql.CreateConnection();

        // Load session header
        var session = await conn.QuerySingleOrDefaultAsync<SessionHeaderRow>("""
            SELECT
                es.Id           AS SessionId,
                es.UserId,
                es.PaperId,
                COALESCE(es.SubjectId, p.SubjectId) AS SubjectId,
                es.TopicId,
                p.Title         AS PaperTitle,
                es.StartTime,
                es.ExpiresAt,
                es.TimeLimitMinutes,
                es.EndTime,
                es.Status,
                es.Mode,
                es.ObtainedMarks,
                es.TotalMarks,
                es.FinalScore   AS Percentage
            FROM ExamSessions es
            LEFT JOIN Papers p ON p.Id = es.PaperId
            WHERE es.Id = @SessionId
            """,
            new { request.SessionId });

        if (session is null)
            throw new NotFoundException("Session not found.");

        if (session.UserId != currentUser.UserId)
            throw new ForbiddenException("Access denied.");

        // Load responses with order from ExamSessionQuestions
        var responses = await conn.QueryAsync<SessionResponseRow>("""
            SELECT
                ur.QuestionId,
                esq.OrderIndex,
                ur.SelectedOptionId,
                ur.IsCorrect,
                ur.MarksAwarded,
                ur.ResponseStatus,
                ur.TimeSpentSeconds
            FROM UserResponses ur
            JOIN ExamSessionQuestions esq
                ON esq.SessionId  = ur.SessionId
               AND esq.QuestionId = ur.QuestionId
            WHERE ur.SessionId = @SessionId
            ORDER BY esq.OrderIndex
            """,
            new { request.SessionId });

        var responseList = responses.Select(r => new SessionResponseDto(
            QuestionId:      r.QuestionId,
            OrderIndex:      r.OrderIndex,
            SelectedOptionId: r.SelectedOptionId,
            IsCorrect:       r.IsCorrect,
            MarksAwarded:    r.MarksAwarded,
            ResponseStatus:  r.ResponseStatus))
            .ToList();

        int correctCount = responseList.Count(r => r.IsCorrect);
        int skippedCount = responseList.Count(r => r.SelectedOptionId is null);
        int wrongCount   = responseList.Count - correctCount - skippedCount;

        var hasReviewData = responseList.Count > 0;
        var activeTimeTaken = responses.Sum(r => Math.Max(0, r.TimeSpentSeconds));
        var wallClockTime = session.EndTime.HasValue
            ? Math.Max(0, (int)(session.EndTime.Value - session.StartTime).TotalSeconds)
            : 0;
        var timeTakenSeconds = activeTimeTaken > 0 ? activeTimeTaken : wallClockTime;
        var affectsMastery = session.Mode is nameof(ExamMode.MockExam) or nameof(ExamMode.TopicExam);

        return new SessionDetailDto(
            SessionId:    session.SessionId,
            PaperId:      session.PaperId,
            SubjectId:    session.SubjectId,
            TopicId:      session.TopicId,
            PaperTitle:   session.PaperTitle,
            StartTime:    session.StartTime,
            ServerNow:    DateTime.UtcNow,
            ExpiresAt:    session.ExpiresAt,
            TimeLimitMinutes: session.TimeLimitMinutes,
            EndTime:      session.EndTime,
            Status:       session.Status,
            Mode:         session.Mode,
            ObtainedMarks: session.ObtainedMarks,
            TotalMarks:   session.TotalMarks,
            Percentage:   session.Percentage,
            IsPassing:    session.Percentage >= 40,
            CorrectCount: correctCount,
            WrongCount:   wrongCount,
            SkippedCount: skippedCount,
            TimeTakenSeconds: timeTakenSeconds,
            AffectsMastery: affectsMastery,
            HasReviewData: hasReviewData,
            ReviewAvailableUntil: session.EndTime?.Add(ReviewRetentionPeriod),
            Responses:    responseList);
    }

    private sealed record SessionHeaderRow(
        Guid SessionId,
        Guid UserId,
        Guid? PaperId,
        Guid? SubjectId,
        Guid? TopicId,
        string? PaperTitle,
        DateTime StartTime,
        DateTime? ExpiresAt,
        int? TimeLimitMinutes,
        DateTime? EndTime,
        string Status,
        string Mode,
        decimal ObtainedMarks,
        decimal TotalMarks,
        decimal Percentage);

    private sealed record SessionResponseRow(
        Guid QuestionId,
        int OrderIndex,
        Guid? SelectedOptionId,
        bool IsCorrect,
        decimal MarksAwarded,
        string ResponseStatus,
        int TimeSpentSeconds);
}
