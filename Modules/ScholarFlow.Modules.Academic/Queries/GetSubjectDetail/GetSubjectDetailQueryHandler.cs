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
                s.NameEnglish AS SubjectNameEnglish,
                s.NameTamil   AS SubjectNameTamil,
                s.NameSinhala AS SubjectNameSinhala,
                s.Description,
                st.Id        AS StreamId,
                st.NameEnglish AS StreamNameEnglish,
                st.NameTamil   AS StreamNameTamil,
                st.NameSinhala AS StreamNameSinhala,
                (SELECT COUNT(*) FROM Topics t WHERE t.SubjectId = s.Id AND t.IsDeleted = 0) AS TopicCount
            FROM Subjects s
            LEFT JOIN SubjectStreams ss ON ss.SubjectId = s.Id
            LEFT JOIN Streams st ON st.Id = ss.StreamId AND st.IsDeleted = 0
            WHERE s.Id = @Id AND s.IsDeleted = 0
            """, new { request.Id });

        var list = rows.AsList();

        if (list.Count == 0)
            throw new NotFoundException("Subject not found.");

        var first = list[0];
        var streams = list
            .Where(r => r.StreamId.HasValue)
            .Select(r => new StreamDto(
                r.StreamId!.Value,
                r.StreamNameEnglish!,
                r.StreamNameTamil,
                r.StreamNameSinhala,
                null))
            .ToList();

        return new SubjectDetailDto(
            first.SubjectId,
            streams,
            first.TopicCount,
            first.SubjectNameEnglish,
            first.SubjectNameTamil,
            first.SubjectNameSinhala,
            first.Description);
    }

    private sealed record SubjectRow(
        Guid SubjectId,
        string SubjectNameEnglish,
        string? SubjectNameTamil,
        string? SubjectNameSinhala,
        string? Description,
        Guid? StreamId,
        string? StreamNameEnglish,
        string? StreamNameTamil,
        string? StreamNameSinhala,
        int TopicCount);
}
