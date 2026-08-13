using Dapper;
using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.Modules.Examination.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Examination.Queries.GetSessionResume;

public sealed class GetSessionResumeQueryHandler(
    IExamSessionRepository examRepo,
    IExplanationRepository explanationRepo,
    ISqlConnectionFactory sql,
    ICurrentUser currentUser)
    : IRequestHandler<GetSessionResumeQuery, ResumeSessionResultDto>
{
    public async Task<ResumeSessionResultDto> Handle(GetSessionResumeQuery request, CancellationToken ct)
    {
        var session = await examRepo.GetByIdWithResponsesAsync(request.SessionId, ct)
            ?? throw new NotFoundException("Exam session not found.");

        if (session.UserId != currentUser.UserId)
        {
            throw new ForbiddenException("Access denied.");
        }

        var now = DateTime.UtcNow;

        if (session.Status == ExamSessionStatus.InProgress && session.HasExpired(now))
        {
            await CompleteTimedOutAsync(session, ct);
            return NotResumable(session, "Exam time has ended.");
        }

        if (session.Status == ExamSessionStatus.InProgress
            && session.Mode.IsPracticeMode()
            && session.LastActivityAt < now.AddHours(-24))
        {
            session.Abandon();
            await examRepo.SaveChangesAsync(ct);
            return NotResumable(session, "Practice resume period has ended.");
        }

        if (session.Status != ExamSessionStatus.InProgress)
        {
            return NotResumable(session, "Session is already closed.");
        }

        var questions = await LoadQuestionsAsync(session, ct);
        var responses = session.UserResponses
            .OrderBy(response => response.OrderIndex)
            .Select(response => new ResumeAnswerDto(
                response.QuestionId,
                response.SelectedOptionId,
                response.TimeSpentSeconds,
                response.ResponseStatus.ToString()))
            .ToList();

        var answeredCount = responses.Count(response => response.SelectedOptionId.HasValue);
        var remainingSeconds = session.ExpiresAt.HasValue
            ? Math.Max(0, (int)Math.Ceiling((session.ExpiresAt.Value - now).TotalSeconds))
            : (int?)null;

        return new ResumeSessionResultDto(
            IsResumable: true,
            Status: session.Status.ToString(),
            Title: await LoadTitleAsync(session, ct),
            Session: new StartSessionResultDto(
                session.Id,
                session.StartTime,
                now,
                session.ExpiresAt,
                session.TimeLimitMinutes,
                session.Mode.ToString(),
                questions.Count,
                questions),
            Responses: responses,
            AnsweredCount: answeredCount,
            TotalQuestions: responses.Count,
            RemainingSeconds: remainingSeconds);
    }

    private async Task<List<ExamQuestionDto>> LoadQuestionsAsync(ExamSession session, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();
        var rows = (await conn.QueryAsync<QuestionRow>("""
            SELECT
                q.Id AS QuestionId,
                esq.OrderIndex,
                q.QuestionText,
                q.QuestionImageUrl,
                q.Marks,
                o.Id AS OptionId,
                o.Label,
                o.OptionText,
                o.OptionImageUrl,
                o.IsCorrect AS OptionIsCorrect
            FROM ExamSessionQuestions esq
            INNER JOIN Questions q ON q.Id = esq.QuestionId AND q.IsDeleted = 0
            LEFT JOIN Options o ON o.QuestionId = q.Id
            WHERE esq.SessionId = @SessionId
            ORDER BY esq.OrderIndex, o.Label
            """, new { SessionId = session.Id }))
            .ToList();

        var questionsDict = new Dictionary<Guid, (int Order, string Text, string? Image, decimal Marks, List<ExamOptionDto> Options, Guid? CorrectOptionId)>();

        foreach (var row in rows)
        {
            if (!questionsDict.TryGetValue(row.QuestionId, out var question))
            {
                question = (row.OrderIndex, row.QuestionText, row.QuestionImageUrl, row.Marks, [], null);
                questionsDict[row.QuestionId] = question;
            }

            if (row.OptionId.HasValue)
            {
                question.Options.Add(new ExamOptionDto(
                    row.OptionId.Value,
                    row.Label ?? string.Empty,
                    row.OptionText ?? string.Empty,
                    row.OptionImageUrl));

                if (row.OptionIsCorrect)
                {
                    questionsDict[row.QuestionId] = (question.Order, question.Text, question.Image, question.Marks, question.Options, row.OptionId.Value);
                }
            }
        }

        var explanationsMap = new Dictionary<Guid, Explanation>();
        if (session.Mode.IsPracticeMode())
        {
            var questionIds = questionsDict.Keys.ToList();
            var explanations = await explanationRepo.GetByQuestionIdsAsync(questionIds, ct);
            explanationsMap = explanations.ToDictionary(explanation => explanation.QuestionId);
        }

        return questionsDict
            .OrderBy(item => item.Value.Order)
            .Select(item =>
            {
                var questionId = item.Key;
                var details = item.Value;

                string? explanationText = null;
                if (session.Mode.IsPracticeMode()
                    && explanationsMap.TryGetValue(questionId, out var explanation)
                    && explanation.Sections.Any())
                {
                    explanationText = string.Join("\n\n", explanation.Sections
                        .OrderBy(section => section.OrderIndex)
                        .Select(section => $"{section.Title}\n{section.Content}"));
                }

                return new ExamQuestionDto(
                    questionId,
                    details.Order,
                    details.Text,
                    details.Image,
                    details.Marks,
                    details.Options.OrderBy(option => option.Label).ToList(),
                    session.Mode.IsPracticeMode() ? details.CorrectOptionId : null,
                    explanationText);
            })
            .ToList();
    }

    private async Task<string?> LoadTitleAsync(ExamSession session, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        return await conn.QueryFirstOrDefaultAsync<string?>("""
            SELECT COALESCE(
                p.Title,
                CASE
                    WHEN sp.Medium = 2 THEN COALESCE(NULLIF(s.NameTamil, N''), s.NameEnglish)
                    WHEN sp.Medium = 1 THEN COALESCE(NULLIF(s.NameSinhala, N''), s.NameEnglish)
                    ELSE s.NameEnglish
                END
            )
            FROM ExamSessions es
            LEFT JOIN Papers p ON p.Id = es.PaperId
            LEFT JOIN Subjects s ON s.Id = es.SubjectId
            LEFT JOIN StudentProfiles sp ON sp.UserId = @UserId
            WHERE es.Id = @SessionId
            """, new { SessionId = session.Id, UserId = currentUser.UserId });
    }

    private async Task CompleteTimedOutAsync(ExamSession session, CancellationToken ct)
    {
        var questionIds = session.UserResponses.Select(response => response.QuestionId).ToList();

        using var conn = sql.CreateConnection();
        var gradingDetails = (await conn.QueryAsync<GradingRow>("""
            SELECT
                q.Id AS QuestionId,
                q.Marks,
                o.Id AS CorrectOptionId
            FROM Questions q
            LEFT JOIN Options o ON o.QuestionId = q.Id AND o.IsCorrect = 1
            WHERE q.Id IN @QuestionIds
              AND q.IsDeleted = 0
            """, new { QuestionIds = questionIds }))
            .ToList();

        var marksPerQuestion = gradingDetails.ToDictionary(row => row.QuestionId, row => row.Marks);
        var correctOptionPerQuestion = gradingDetails.ToDictionary(
            row => row.QuestionId,
            row => row.CorrectOptionId ?? Guid.Empty);

        session.Complete(marksPerQuestion, correctOptionPerQuestion, ExamSessionStatus.TimedOut);
        await examRepo.SaveChangesAsync(ct);
    }

    private static ResumeSessionResultDto NotResumable(ExamSession session, string title)
        => new(
            IsResumable: false,
            Status: session.Status.ToString(),
            Title: title,
            Session: null,
            Responses: [],
            AnsweredCount: session.UserResponses.Count(response => response.SelectedOptionId.HasValue),
            TotalQuestions: session.UserResponses.Count,
            RemainingSeconds: 0);

    private sealed class QuestionRow
    {
        public Guid QuestionId { get; set; }
        public int OrderIndex { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string? QuestionImageUrl { get; set; }
        public decimal Marks { get; set; }
        public Guid? OptionId { get; set; }
        public string? Label { get; set; }
        public string? OptionText { get; set; }
        public string? OptionImageUrl { get; set; }
        public bool OptionIsCorrect { get; set; }
    }

    private sealed class GradingRow
    {
        public Guid QuestionId { get; set; }
        public decimal Marks { get; set; }
        public Guid? CorrectOptionId { get; set; }
    }
}
