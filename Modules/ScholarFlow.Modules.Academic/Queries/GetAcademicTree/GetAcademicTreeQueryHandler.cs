using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Academic.DTOs;

namespace ScholarFlow.Modules.Academic.Queries.GetAcademicTree;

public sealed class GetAcademicTreeQueryHandler(ISqlConnectionFactory sql)
    : IRequestHandler<GetAcademicTreeQuery, List<AcademicStreamTreeDto>>
{
    public async Task<List<AcademicStreamTreeDto>> Handle(GetAcademicTreeQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var rows = await conn.QueryAsync<TreeRow>("""
            SELECT
                st.Id           AS StreamId,
                st.Name         AS StreamName,
                st.Description  AS StreamDescription,
                s.Id            AS SubjectId,
                s.Name          AS SubjectName,
                t.Id            AS TopicId,
                t.TopicName,
                t.OrderIndex    AS TopicOrder,
                sub.Id          AS SubTopicId,
                sub.SubTopicName,
                sub.OrderIndex  AS SubTopicOrder
            FROM AcademicStreams st
            LEFT JOIN SubjectStreams ss  ON ss.StreamId  = st.Id
            LEFT JOIN Subjects s        ON s.Id          = ss.SubjectId  AND s.IsDeleted  = 0
            LEFT JOIN Topics t          ON t.SubjectId   = s.Id          AND t.IsDeleted  = 0
            LEFT JOIN SubTopics sub     ON sub.TopicId   = t.Id          AND sub.IsDeleted = 0
            WHERE st.IsDeleted = 0
            ORDER BY st.Name, s.Name, t.OrderIndex, sub.OrderIndex
            """);

        var streams  = new Dictionary<Guid, (string Name, string? Desc, Dictionary<Guid, (string Name, Dictionary<Guid, (string Name, int Order, List<SubTopicDto> Subs)> Topics)> Subjects)>();

        foreach (var row in rows)
        {
            if (!streams.ContainsKey(row.StreamId))
                streams[row.StreamId] = (row.StreamName, row.StreamDescription, []);

            if (!row.SubjectId.HasValue) continue;
            var subjects = streams[row.StreamId].Subjects;

            if (!subjects.ContainsKey(row.SubjectId.Value))
                subjects[row.SubjectId.Value] = (row.SubjectName!, []);

            if (!row.TopicId.HasValue) continue;
            var topics = subjects[row.SubjectId.Value].Topics;

            if (!topics.ContainsKey(row.TopicId.Value))
                topics[row.TopicId.Value] = (row.TopicName!, row.TopicOrder, []);

            if (row.SubTopicId.HasValue)
                topics[row.TopicId.Value].Subs.Add(
                    new SubTopicDto(row.SubTopicId.Value, row.SubTopicName!, row.SubTopicOrder));
        }

        return streams.Select(st => new AcademicStreamTreeDto(
            st.Key,
            st.Value.Name,
            st.Value.Desc,
            st.Value.Subjects.Select(s => new SubjectTreeDto(
                s.Key,
                s.Value.Name,
                s.Value.Topics.Select(t => new TopicWithSubTopicsDto(
                    t.Key, t.Value.Name, t.Value.Order, t.Value.Subs))
                .ToList()))
            .ToList()))
        .ToList();
    }

    private sealed record TreeRow(
        Guid StreamId, string StreamName, string? StreamDescription,
        Guid? SubjectId, string? SubjectName,
        Guid? TopicId, string? TopicName, int TopicOrder,
        Guid? SubTopicId, string? SubTopicName, int SubTopicOrder);
}
