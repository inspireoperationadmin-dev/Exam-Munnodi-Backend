using Dapper;
using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Academic.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Queries.GetExplanationByQuestionId;

public sealed class GetExplanationByQuestionIdQueryHandler(
    ISqlConnectionFactory sql,
    ICurrentUser currentUser)
    : IRequestHandler<GetExplanationByQuestionIdQuery, ExplanationDto?>
{
    public async Task<ExplanationDto?> Handle(GetExplanationByQuestionIdQuery request, CancellationToken ct)
    {
        if (currentUser.IsInRole(AppRole.Student))
        {
            throw new ForbiddenException("Students must use the examination review or practice endpoints.");
        }

        using var conn = sql.CreateConnection();

        var rows = await conn.QueryAsync<ExplanationRow>("""
            SELECT
                e.Id          AS ExplanationId,
                e.QuestionId,
                e.Type,
                e.VideoUrl,
                o.Id          AS CorrectOptionId,
                es.Id         AS SectionId,
                es.Title,
                es.Content,
                es.OrderIndex
            FROM Explanations e
            LEFT JOIN Options o ON o.QuestionId = e.QuestionId AND o.IsCorrect = CAST(1 AS BIT)
            LEFT JOIN ExplanationSections es ON es.ExplanationId = e.Id
            WHERE e.QuestionId = @QuestionId
            ORDER BY es.OrderIndex
            """, new { request.QuestionId });

        var list = rows.AsList();
        if (list.Count == 0) return null;

        var first    = list[0];
        var sections = list
            .Where(r => r.SectionId.HasValue)
            .Select(r => new ExplanationSectionDto(
                r.SectionId!.Value,
                r.Title ?? string.Empty,
                r.Content ?? string.Empty,
                r.OrderIndex))
            .ToList();

        return new ExplanationDto(
            first.ExplanationId,
            first.QuestionId,
            first.Type,
            first.VideoUrl,
            first.CorrectOptionId,
            sections);
    }

    private sealed record ExplanationRow(
        Guid    ExplanationId,
        Guid    QuestionId,
        string  Type,
        string? VideoUrl,
        Guid?   CorrectOptionId,   // ← add
        Guid?   SectionId,
        string? Title,
        string? Content,
        int     OrderIndex);
}
