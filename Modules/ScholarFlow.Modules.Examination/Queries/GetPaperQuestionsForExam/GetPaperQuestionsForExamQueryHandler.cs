using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Queries.GetPaperQuestionsForExam;

public sealed class GetPaperQuestionsForExamQueryHandler(
    ISqlConnectionFactory sql)
    : IRequestHandler<GetPaperQuestionsForExamQuery, List<ExamQuestionDto>>
{
    public async Task<List<ExamQuestionDto>> Handle(GetPaperQuestionsForExamQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var rows = await conn.QueryAsync<QuestionRow>("""
            SELECT
                q.Id            AS QuestionId,
                q.OrderIndex,
                q.QuestionText,
                q.Marks,
                q.QuestionImageUrl,
                o.Id            AS OptionId,
                o.Label,
                o.OptionText,
                
                o.OptionImageUrl
            FROM Questions q
            LEFT JOIN Options o ON o.QuestionId = q.Id
            WHERE q.PaperId   = @PaperId
              AND q.IsDeleted  = 0
            ORDER BY q.OrderIndex, o.Label
            """,
            new { request.PaperId });

        var questions = new Dictionary<Guid, (int Order, string Text, string? Image, decimal Marks, List<ExamOptionDto> Options)>();

        foreach (var row in rows)
        {
            if (!questions.TryGetValue(row.QuestionId, out var q))
            {
                q = (row.OrderIndex, row.QuestionText, row.QuestionImageUrl, row.Marks, []);
                questions[row.QuestionId] = q;
            }

            if (row.OptionId.HasValue)
            {
                q.Options.Add(new ExamOptionDto(
                    Id:             row.OptionId.Value,
                    Label:          row.Label ?? string.Empty,
                    OptionText:     row.OptionText ?? string.Empty,
                    OptionImageUrl: row.OptionImageUrl));
            }
        }

        return questions
            .Select(kv => new ExamQuestionDto(
                Id:              kv.Key,
                OrderIndex:      kv.Value.Order,
                QuestionText:    kv.Value.Text,
                QuestionImageUrl: kv.Value.Image,
                Marks:           kv.Value.Marks,              // ← added
                Options:         kv.Value.Options))
            .OrderBy(q => q.OrderIndex)
            .ToList();
    }

    private sealed class QuestionRow
    {
        public Guid QuestionId { get; set; }
        public int OrderIndex { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public decimal Marks { get; set; }
        public string? QuestionImageUrl { get; set; }
        public Guid? OptionId { get; set; }
        public string? Label { get; set; }
        public string? OptionText { get; set; }
        public string? OptionImageUrl { get; set; }
    }
}
