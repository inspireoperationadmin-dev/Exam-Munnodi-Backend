using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Academic.DTOs;

namespace ScholarFlow.Modules.Academic.Queries.GetPaperQuestions;

public sealed class GetPaperQuestionsQueryHandler(ISqlConnectionFactory sql)
    : IRequestHandler<GetPaperQuestionsQuery, List<QuestionWithOptionsDto>>
{
    public async Task<List<QuestionWithOptionsDto>> Handle(GetPaperQuestionsQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var rows = await conn.QueryAsync<QuestionRow>("""
            SELECT
                q.Id             AS QuestionId,
                q.OrderIndex,
                q.QuestionText,
                q.QuestionImageUrl,
                q.SubTopicId,
                st.SubTopicName,
                CAST(CASE WHEN e.Id IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasExplanation,
                q.Marks,
                o.Id             AS OptionId,
                o.Label,
                o.OptionText,
                o.OptionImageUrl,
                o.IsCorrect
            FROM Questions q
            JOIN SubTopics st   ON st.Id = q.SubTopicId
            LEFT JOIN Explanations e ON e.QuestionId = q.Id
            LEFT JOIN Options o ON o.QuestionId = q.Id
            WHERE q.PaperId = @PaperId AND q.IsDeleted = 0
            ORDER BY q.OrderIndex, o.Label
            """, new { request.PaperId });

        var questions = new Dictionary<Guid, (QuestionRow Q, List<OptionDto> Opts)>();

        foreach (var row in rows)
        {
            if (!questions.ContainsKey(row.QuestionId))
                questions[row.QuestionId] = (row, []);

            if (row.OptionId.HasValue)
                questions[row.QuestionId].Opts.Add(
                    new OptionDto(row.OptionId.Value, row.Label!, row.OptionText!, row.OptionImageUrl, row.IsCorrect));
        }

        return questions.Values
            .Select(entry => new QuestionWithOptionsDto(
                entry.Q.QuestionId,
                entry.Q.OrderIndex,
                entry.Q.QuestionText,
                entry.Q.QuestionImageUrl,
                entry.Q.SubTopicId,
                entry.Q.SubTopicName,
                entry.Q.HasExplanation,
                entry.Q.Marks,
                entry.Opts))
            .ToList();
    }

    private sealed record QuestionRow(
        Guid QuestionId, int OrderIndex, string QuestionText, string? QuestionImageUrl,
        Guid SubTopicId, string SubTopicName, bool HasExplanation, decimal Marks,
        Guid? OptionId, string? Label, string? OptionText, string? OptionImageUrl, bool IsCorrect);
}
