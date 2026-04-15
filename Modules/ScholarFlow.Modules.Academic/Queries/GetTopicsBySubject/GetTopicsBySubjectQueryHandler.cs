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
                t.TopicName,
                t.OrderIndex  AS TopicOrder,
                st.Id         AS SubTopicId,
                st.SubTopicName,
                st.OrderIndex AS SubTopicOrder
            FROM Topics t
            LEFT JOIN SubTopics st ON st.TopicId = t.Id AND st.IsDeleted = 0
            WHERE t.SubjectId = @SubjectId AND t.IsDeleted = 0
            ORDER BY t.OrderIndex, st.OrderIndex
            """, new { request.SubjectId });

        var topics = new Dictionary<Guid, (string Name, int Order, List<SubTopicDto> Subs)>();

        foreach (var row in rows)
        {
            if (!topics.ContainsKey(row.TopicId))
                topics[row.TopicId] = (row.TopicName, row.TopicOrder, []);

            if (row.SubTopicId.HasValue)
                topics[row.TopicId].Subs.Add(new SubTopicDto(row.SubTopicId.Value, row.SubTopicName!, row.SubTopicOrder));
        }

        return topics
            .Select(kv => new TopicWithSubTopicsDto(kv.Key, kv.Value.Name, kv.Value.Order, kv.Value.Subs))
            .ToList();
    }

    private sealed record TopicRow(
        Guid TopicId, string TopicName, int TopicOrder,
        Guid? SubTopicId, string? SubTopicName, int SubTopicOrder);
}
