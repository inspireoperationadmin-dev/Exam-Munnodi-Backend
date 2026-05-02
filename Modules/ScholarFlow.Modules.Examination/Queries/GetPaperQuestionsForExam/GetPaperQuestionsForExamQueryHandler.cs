using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Examination.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

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
            JOIN Options o ON o.QuestionId = q.Id
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

            q.Options.Add(new ExamOptionDto(
                Id:             row.OptionId,
                Label:          row.Label,
                OptionText:     row.OptionText,
                OptionImageUrl: row.OptionImageUrl));
        }

        if (questions.Count == 0)
            throw new NotFoundException("Paper not found or has no questions.");

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

    private sealed record QuestionRow(
        Guid QuestionId,
        int OrderIndex,
        string QuestionText,
        string? QuestionImageUrl,
        Guid OptionId,
        string Label,
        string OptionText,
        decimal Marks,
        string? OptionImageUrl);
}
