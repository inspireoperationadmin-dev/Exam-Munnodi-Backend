using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Academic.DTOs;

namespace ScholarFlow.Modules.Academic.Queries.GetPapers;

public sealed class GetPapersQueryHandler(ISqlConnectionFactory sql)
    : IRequestHandler<GetPapersQuery, List<PaperSummaryDto>>
{
    public async Task<List<PaperSummaryDto>> Handle(GetPapersQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var where = new List<string> { "p.IsDeleted = 0" };
        if (request.SubjectId.HasValue) where.Add("p.SubjectId = @SubjectId");
        if (request.Type.HasValue)      where.Add("p.Type = @Type");
        if (request.Medium.HasValue)    where.Add("p.Medium = @Medium");
        if (request.Year.HasValue)      where.Add("p.Year = @Year");

        var sql_ = $"""
            SELECT
                p.Id,
                p.Title,
                ISNULL(s.Name, '') AS SubjectName,
                p.Type,
                p.Medium,
                p.Year,
                p.Sitting,
                (SELECT COUNT(*) FROM Questions q WHERE q.PaperId = p.Id AND q.IsDeleted = 0) AS QuestionCount,
                p.IsPublic,
                p.CreatedAt
            FROM Papers p
            LEFT JOIN Subjects s ON s.Id = p.SubjectId AND s.IsDeleted = 0
            WHERE {string.Join(" AND ", where)}
            ORDER BY p.Year DESC, p.Title
            """;

        var rows = await conn.QueryAsync<PaperSummaryDto>(sql_, new
        {
            request.SubjectId,
            Type   = request.Type?.ToString(),
            Medium = request.Medium?.ToString(),
            request.Year
        });

        return rows.AsList();
    }
}
