using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Academic.DTOs;

namespace ScholarFlow.Modules.Academic.Queries.GetAllSubjects;

public sealed class GetAllSubjectsQueryHandler(ISqlConnectionFactory sql)
    : IRequestHandler<GetAllSubjectsQuery, List<SubjectDetailDto>>
{
    public async Task<List<SubjectDetailDto>> Handle(
        GetAllSubjectsQuery request,
        CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var streamFilter = request.StreamId.HasValue
            ? "AND EXISTS (SELECT 1 FROM SubjectStreams ss2 WHERE ss2.SubjectId = s.Id AND ss2.StreamId = @StreamId)"
            : string.Empty;

        var query = $"""
            SELECT
                s.Id             AS SubjectId,
                s.NameEnglish    AS SubjectNameEnglish,
                s.NameTamil      AS SubjectNameTamil,
                s.NameSinhala    AS SubjectNameSinhala,
                s.Description    AS SubjectDescription,
                (
                    SELECT COUNT(*)
                    FROM Topics t
                    WHERE t.SubjectId = s.Id
                      AND t.IsDeleted = 0
                ) AS TopicCount,
                st.Id            AS StreamId,
                st.NameEnglish   AS StreamNameEnglish,
                st.NameTamil     AS StreamNameTamil,
                st.NameSinhala   AS StreamNameSinhala,
                st.Description   AS StreamDescription
            FROM Subjects s
            LEFT JOIN SubjectStreams ss ON ss.SubjectId = s.Id
            LEFT JOIN Streams st       ON st.Id = ss.StreamId
                                      AND st.IsDeleted = 0
            WHERE s.IsDeleted = 0
            {streamFilter}
            ORDER BY s.NameEnglish, st.NameEnglish
            """;

        var subjectMap = new Dictionary<Guid, SubjectDetailDto>();

        await conn.QueryAsync<SubjectRow, StreamRow?, SubjectDetailDto>(
            query,
            map: (subjectRow, streamRow) =>
            {
                if (!subjectMap.TryGetValue(subjectRow.SubjectId, out var dto))
                {
                    dto = new SubjectDetailDto(
                        subjectRow.SubjectId,
                        new List<StreamDto>(),
                        subjectRow.TopicCount,
                        subjectRow.SubjectNameEnglish,
                        subjectRow.SubjectNameTamil,
                        subjectRow.SubjectNameSinhala,
                        subjectRow.SubjectDescription);

                    subjectMap[subjectRow.SubjectId] = dto;
                }

                if (streamRow is { StreamId: not null })
                {
                    ((List<StreamDto>)dto.Streams).Add(
                        new StreamDto(
                            streamRow.StreamId!.Value,
                            streamRow.StreamNameEnglish!,
                            streamRow.StreamNameTamil,
                            streamRow.StreamNameSinhala,
                            streamRow.StreamDescription));
                }

                return dto;
            },
            param: new { request.StreamId },
            splitOn: "StreamId");

        return subjectMap.Values.ToList();
    }

    // ── Private Dapper row types ───────────────────────────────────────────────

    private sealed class SubjectRow
    {
        public Guid    SubjectId          { get; init; }
        public string  SubjectNameEnglish { get; init; } = string.Empty;
        public string? SubjectNameTamil   { get; init; }
        public string? SubjectNameSinhala { get; init; }
        public string? SubjectDescription { get; init; }
        public int     TopicCount         { get; init; }
    }

    private sealed class StreamRow
    {
        public Guid?   StreamId          { get; init; }
        public string? StreamNameEnglish { get; init; }
        public string? StreamNameTamil   { get; init; }
        public string? StreamNameSinhala { get; init; }
        public string? StreamDescription { get; init; }
    }
}
