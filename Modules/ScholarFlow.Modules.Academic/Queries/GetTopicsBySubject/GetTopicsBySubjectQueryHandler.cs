using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Academic.DTOs;

namespace ScholarFlow.Modules.Academic.Queries.GetTopicsBySubject;

public sealed class GetTopicsBySubjectQueryHandler(ISqlConnectionFactory sql)
    : IRequestHandler<GetTopicsBySubjectQuery, List<TopicWithSubTopicsDto>>
{
    public async Task<List<TopicWithSubTopicsDto>> Handle(GetTopicsBySubjectQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var rows = await conn.QueryAsync<TopicRow>("""
            SELECT
                t.Id          AS TopicId,
                t.NameEnglish AS TopicNameEnglish,
                t.NameTamil   AS TopicNameTamil,
                t.NameSinhala AS TopicNameSinhala,
                t.OrderIndex  AS TopicOrder,
                st.Id         AS SubTopicId,
                st.NameEnglish AS SubTopicNameEnglish,
                st.NameTamil   AS SubTopicNameTamil,
                st.NameSinhala AS SubTopicNameSinhala,
                st.OrderIndex AS SubTopicOrder
            FROM Topics t
            LEFT JOIN SubTopics st ON st.TopicId = t.Id AND st.IsDeleted = 0
            WHERE t.SubjectId = @SubjectId AND t.IsDeleted = 0
            ORDER BY t.OrderIndex, st.OrderIndex
            """, new { request.SubjectId });

        var topics = new Dictionary<Guid, (string English, int Order, string? Tamil, string? Sinhala, List<SubTopicDto> Subs)>();

        foreach (var row in rows)
        {
            if (!topics.ContainsKey(row.TopicId))
                topics[row.TopicId] = (
                    row.TopicNameEnglish,
                    row.TopicOrder,
                    row.TopicNameTamil,
                    row.TopicNameSinhala,
                    []);

            if (row.SubTopicId.HasValue)
                topics[row.TopicId].Subs.Add(new SubTopicDto(
                    row.SubTopicId.Value,
                    row.SubTopicOrder,
                    row.SubTopicNameEnglish!,
                    row.SubTopicNameTamil,
                    row.SubTopicNameSinhala));
        }

        return topics
            .Select(kv => new TopicWithSubTopicsDto(
                kv.Key,
                kv.Value.Order,
                kv.Value.Subs,
                kv.Value.English,
                kv.Value.Tamil,
                kv.Value.Sinhala))
            .ToList();
    }

    private sealed record TopicRow(
        Guid TopicId,
        string TopicNameEnglish,
        string? TopicNameTamil,
        string? TopicNameSinhala,
        int TopicOrder,
        Guid? SubTopicId,
        string? SubTopicNameEnglish,
        string? SubTopicNameTamil,
        string? SubTopicNameSinhala,
        int SubTopicOrder);
}
