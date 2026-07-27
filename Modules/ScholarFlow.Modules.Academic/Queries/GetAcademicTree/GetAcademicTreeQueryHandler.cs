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
                st.NameEnglish  AS StreamNameEnglish,
                st.NameTamil    AS StreamNameTamil,
                st.NameSinhala  AS StreamNameSinhala,
                st.Description  AS StreamDescription,
                s.Id            AS SubjectId,
                s.NameEnglish   AS SubjectNameEnglish,
                s.NameTamil     AS SubjectNameTamil,
                s.NameSinhala   AS SubjectNameSinhala,
                t.Id            AS TopicId,
                t.NameEnglish   AS TopicNameEnglish,
                t.NameTamil     AS TopicNameTamil,
                t.NameSinhala   AS TopicNameSinhala,
                t.OrderIndex    AS TopicOrder,
                sub.Id          AS SubTopicId,
                sub.NameEnglish AS SubTopicNameEnglish,
                sub.NameTamil   AS SubTopicNameTamil,
                sub.NameSinhala AS SubTopicNameSinhala,
                sub.OrderIndex  AS SubTopicOrder
            FROM Streams st
            LEFT JOIN SubjectStreams ss  ON ss.StreamId  = st.Id
            LEFT JOIN Subjects s        ON s.Id          = ss.SubjectId  AND s.IsDeleted  = 0
            LEFT JOIN Topics t          ON t.SubjectId   = s.Id          AND t.IsDeleted  = 0
            LEFT JOIN SubTopics sub     ON sub.TopicId   = t.Id          AND sub.IsDeleted = 0
            WHERE st.IsDeleted = 0
            ORDER BY st.NameEnglish, s.NameEnglish, t.OrderIndex, sub.OrderIndex
            """);

        var streams = new Dictionary<Guid, StreamTreeBuilder>();

        foreach (var row in rows)
        {
            if (!streams.ContainsKey(row.StreamId))
            {
                streams[row.StreamId] = new StreamTreeBuilder(
                    row.StreamDescription,
                    row.StreamNameEnglish,
                    row.StreamNameTamil,
                    row.StreamNameSinhala);
            }

            if (!row.SubjectId.HasValue) continue;
            var subjects = streams[row.StreamId].Subjects;

            if (!subjects.ContainsKey(row.SubjectId.Value))
            {
                subjects[row.SubjectId.Value] = new SubjectTreeBuilder(
                    row.SubjectNameEnglish!,
                    row.SubjectNameTamil,
                    row.SubjectNameSinhala);
            }

            if (!row.TopicId.HasValue) continue;
            var topics = subjects[row.SubjectId.Value].Topics;

            if (!topics.ContainsKey(row.TopicId.Value))
            {
                topics[row.TopicId.Value] = new TopicTreeBuilder(
                    row.TopicOrder,
                    row.TopicNameEnglish!,
                    row.TopicNameTamil,
                    row.TopicNameSinhala);
            }

            if (row.SubTopicId.HasValue)
            {
                topics[row.TopicId.Value].Subs.Add(new SubTopicDto(
                    row.SubTopicId.Value,
                    row.SubTopicOrder,
                    row.SubTopicNameEnglish!,
                    row.SubTopicNameTamil,
                    row.SubTopicNameSinhala));
            }
        }

        return streams.Select(st => new AcademicStreamTreeDto(
            st.Key,
            st.Value.Subjects.Select(s => new SubjectTreeDto(
                s.Key,
                s.Value.Topics.Select(t => new TopicWithSubTopicsDto(
                    t.Key,
                    t.Value.Order,
                    t.Value.Subs,
                    t.Value.English,
                    t.Value.Tamil,
                    t.Value.Sinhala))
                .ToList(),
                s.Value.English,
                s.Value.Tamil,
                s.Value.Sinhala))
            .ToList(),
            st.Value.English,
            st.Value.Tamil,
            st.Value.Sinhala,
            st.Value.Desc))
        .ToList();
    }

    private sealed record TreeRow(
        Guid StreamId,
        string StreamNameEnglish,
        string? StreamNameTamil,
        string? StreamNameSinhala,
        string? StreamDescription,
        Guid? SubjectId,
        string? SubjectNameEnglish,
        string? SubjectNameTamil,
        string? SubjectNameSinhala,
        Guid? TopicId,
        string? TopicNameEnglish,
        string? TopicNameTamil,
        string? TopicNameSinhala,
        int TopicOrder,
        Guid? SubTopicId,
        string? SubTopicNameEnglish,
        string? SubTopicNameTamil,
        string? SubTopicNameSinhala,
        int SubTopicOrder);

    private sealed class StreamTreeBuilder(
        string? desc,
        string english,
        string? tamil,
        string? sinhala)
    {
        public string? Desc { get; } = desc;
        public string English { get; } = english;
        public string? Tamil { get; } = tamil;
        public string? Sinhala { get; } = sinhala;
        public Dictionary<Guid, SubjectTreeBuilder> Subjects { get; } = [];
    }

    private sealed class SubjectTreeBuilder(
        string english,
        string? tamil,
        string? sinhala)
    {
        public string English { get; } = english;
        public string? Tamil { get; } = tamil;
        public string? Sinhala { get; } = sinhala;
        public Dictionary<Guid, TopicTreeBuilder> Topics { get; } = [];
    }

    private sealed class TopicTreeBuilder(
        int order,
        string english,
        string? tamil,
        string? sinhala)
    {
        public int Order { get; } = order;
        public string English { get; } = english;
        public string? Tamil { get; } = tamil;
        public string? Sinhala { get; } = sinhala;
        public List<SubTopicDto> Subs { get; } = [];
    }
}
