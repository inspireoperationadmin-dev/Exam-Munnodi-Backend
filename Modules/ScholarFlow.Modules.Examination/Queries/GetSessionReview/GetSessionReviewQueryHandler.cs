using Dapper;
using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Examination.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Examination.Queries.GetSessionReview;

public sealed class GetSessionReviewQueryHandler(
    ISqlConnectionFactory sql,
    ICurrentUser currentUser)
    : IRequestHandler<GetSessionReviewQuery, List<SessionReviewItemDto>>
{
    public async Task<List<SessionReviewItemDto>> Handle(GetSessionReviewQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        // Verify session ownership and Completed status
        var sessionCheck = await conn.QuerySingleOrDefaultAsync<(Guid UserId, string Status)>("""
            SELECT es.UserId, es.Status FROM ExamSessions es WHERE es.Id = @SessionId
            """,
            new { request.SessionId });

        if (sessionCheck == default)
            throw new NotFoundException("Session not found.");

        if (sessionCheck.UserId != currentUser.UserId)
            throw new ForbiddenException("Access denied.");

        if (sessionCheck.Status != nameof(ExamSessionStatus.Completed))
            throw new BadRequestException("Review is only available after the session is completed.");

        // Load all review data in one query
        var rows = await conn.QueryAsync<ReviewRow>("""
            SELECT
                esq.OrderIndex,
                q.Id            AS QuestionId,
                q.QuestionText,
                q.QuestionImageUrl,
                ur.SelectedOptionId,
                ur.IsCorrect,
                ur.MarksAwarded,
                ur.ResponseStatus,
                o.Id            AS OptionId,
                o.Label,
                o.OptionText,
                o.OptionImageUrl,
                o.IsCorrect     AS OptionIsCorrect,
                e.Type          AS ExplanationType,
                e.VideoUrl,
                es2.Title       AS SectionTitle,
                es2.Content     AS SectionContent,
                es2.OrderIndex  AS SectionOrder
            FROM ExamSessionQuestions esq
            JOIN Questions q            ON q.Id  = esq.QuestionId
            JOIN UserResponses ur       ON ur.SessionId = esq.SessionId AND ur.QuestionId = q.Id
            JOIN Options o              ON o.QuestionId = q.Id
            LEFT JOIN Explanations e    ON e.QuestionId = q.Id
            LEFT JOIN ExplanationSections es2 ON es2.ExplanationId = e.Id
            WHERE esq.SessionId = @SessionId
              AND q.IsDeleted   = 0
            ORDER BY esq.OrderIndex, o.Label, es2.OrderIndex
            """,
            new { request.SessionId });

        // Group into review items
        var questions = new Dictionary<Guid, ReviewItemBuilder>();

        foreach (var row in rows)
        {
            if (!questions.TryGetValue(row.QuestionId, out var builder))
            {
                builder = new ReviewItemBuilder(
                    OrderIndex:       row.OrderIndex,
                    QuestionId:       row.QuestionId,
                    QuestionText:     row.QuestionText,
                    QuestionImageUrl: row.QuestionImageUrl,
                    SelectedOptionId: row.SelectedOptionId,
                    IsCorrect:        row.IsCorrect,
                    MarksAwarded:     row.MarksAwarded,
                    ResponseStatus:   row.ResponseStatus);
                questions[row.QuestionId] = builder;
            }

            // Add option (de-duplicate by OptionId)
            if (builder.Options.All(o => o.Id != row.OptionId))
            {
                builder.Options.Add(new ReviewOptionDto(
                    Id:             row.OptionId,
                    Label:          row.Label,
                    OptionText:     row.OptionText,
                    OptionImageUrl: row.OptionImageUrl,
                    IsCorrect:      row.OptionIsCorrect));
            }

            // Set explanation
            if (row.ExplanationType is not null && builder.Explanation is null)
            {
                builder.Explanation = new ReviewExplanationDto(
                    Type:     row.ExplanationType,
                    VideoUrl: row.VideoUrl,
                    Sections: []);
            }

            // Add explanation section (de-duplicate)
            if (row.SectionTitle is not null && builder.Explanation is not null
                && builder.Explanation.Sections.All(s => s.OrderIndex != row.SectionOrder))
            {
                builder.Explanation.Sections.Add(new ReviewExplanationSectionDto(
                    Title:      row.SectionTitle,
                    Content:    row.SectionContent!,
                    OrderIndex: row.SectionOrder!.Value));
            }
        }

        // Find correct option per question
        return questions.Values
            .OrderBy(b => b.OrderIndex)
            .Select(b => new SessionReviewItemDto(
                OrderIndex:       b.OrderIndex,
                QuestionId:       b.QuestionId,
                QuestionText:     b.QuestionText,
                QuestionImageUrl: b.QuestionImageUrl,
                SelectedOptionId: b.SelectedOptionId,
                CorrectOptionId:  b.Options.FirstOrDefault(o => o.IsCorrect)?.Id,
                IsCorrect:        b.IsCorrect,
                MarksAwarded:     b.MarksAwarded,
                ResponseStatus:   b.ResponseStatus,
                Options:          b.Options.OrderBy(o => o.Label).ToList(),
                Explanation:      b.Explanation))
            .ToList();
    }

    private sealed class ReviewItemBuilder(
        int OrderIndex,
        Guid QuestionId,
        string QuestionText,
        string? QuestionImageUrl,
        Guid? SelectedOptionId,
        bool IsCorrect,
        decimal MarksAwarded,
        string ResponseStatus)
    {
        public int OrderIndex { get; } = OrderIndex;
        public Guid QuestionId { get; } = QuestionId;
        public string QuestionText { get; } = QuestionText;
        public string? QuestionImageUrl { get; } = QuestionImageUrl;
        public Guid? SelectedOptionId { get; } = SelectedOptionId;
        public bool IsCorrect { get; } = IsCorrect;
        public decimal MarksAwarded { get; } = MarksAwarded;
        public string ResponseStatus { get; } = ResponseStatus;
        public List<ReviewOptionDto> Options { get; } = [];
        public ReviewExplanationDto? Explanation { get; set; }
    }

    private sealed record ReviewRow(
        int OrderIndex,
        Guid QuestionId,
        string QuestionText,
        string? QuestionImageUrl,
        Guid? SelectedOptionId,
        bool IsCorrect,
        decimal MarksAwarded,
        string ResponseStatus,
        Guid OptionId,
        string Label,
        string OptionText,
        string? OptionImageUrl,
        bool OptionIsCorrect,
        string? ExplanationType,
        string? VideoUrl,
        string? SectionTitle,
        string? SectionContent,
        int? SectionOrder);
}
