using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Academic.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Queries.GetSubjectDetail;

public sealed class GetSubjectDetailQueryHandler(ISqlConnectionFactory sql)
    : IRequestHandler<GetSubjectDetailQuery, SubjectDetailDto>
{
    public async Task<SubjectDetailDto> Handle(GetSubjectDetailQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        // Subject + its streams in one query
        var rows = await conn.QueryAsync<SubjectRow>("""
            SELECT
                s.Id         AS SubjectId,
                s.Name       AS SubjectName,
                s.Description,
                st.Id        AS StreamId,
                st.Name      AS StreamName,
                (SELECT COUNT(*) FROM Topics t WHERE t.SubjectId = s.Id AND t.IsDeleted = 0) AS TopicCount
            FROM Subjects s
            LEFT JOIN SubjectStreams ss ON ss.SubjectId = s.Id
            LEFT JOIN AcademicStreams st ON st.Id = ss.StreamId AND st.IsDeleted = 0
            WHERE s.Id = @Id AND s.IsDeleted = 0
            """, new { request.Id });

        var list = rows.AsList();

        if (list.Count == 0)
            throw new NotFoundException("Subject not found.");

        var first = list[0];
        var streams = list
            .Where(r => r.StreamId.HasValue)
            .Select(r => new StreamDto(r.StreamId!.Value, r.StreamName!, null))
            .ToList();

        return new SubjectDetailDto(
            first.SubjectId,
            first.SubjectName,
            first.Description,
            streams,
            first.TopicCount);
    }

    private sealed record SubjectRow(
        Guid SubjectId, string SubjectName, string? Description,
        Guid? StreamId, string? StreamName, int TopicCount);
}
