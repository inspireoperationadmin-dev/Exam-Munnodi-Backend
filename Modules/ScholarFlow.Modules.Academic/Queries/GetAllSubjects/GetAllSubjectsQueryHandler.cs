using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Academic.DTOs;

namespace ScholarFlow.Modules.Academic.Queries.GetAllSubjects;

public sealed class GetAllSubjectsQueryHandler(ISqlConnectionFactory sql)
    : IRequestHandler<GetAllSubjectsQuery, List<SubjectSummaryDto>>
{
    public async Task<List<SubjectSummaryDto>> Handle(GetAllSubjectsQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var streamFilter = request.StreamId.HasValue
            ? "AND EXISTS (SELECT 1 FROM SubjectStreams ss WHERE ss.SubjectId = s.Id AND ss.StreamId = @StreamId)"
            : string.Empty;

        var sql_ = $"""
            SELECT
                s.Id,
                s.Name,
                s.Description,
                (SELECT COUNT(*) FROM Topics t WHERE t.SubjectId = s.Id AND t.IsDeleted = 0) AS TopicCount
            FROM Subjects s
            WHERE s.IsDeleted = 0
            {streamFilter}
            ORDER BY s.Name
            """;

        var rows = await conn.QueryAsync<SubjectSummaryDto>(sql_, new { request.StreamId });
        return rows.AsList();
    }
}
