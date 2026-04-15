using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Academic.DTOs;

namespace ScholarFlow.Modules.Academic.Queries.GetAllStreams;

public sealed class GetAllStreamsQueryHandler(ISqlConnectionFactory sql)
    : IRequestHandler<GetAllStreamsQuery, List<StreamDto>>
{
    public async Task<List<StreamDto>> Handle(GetAllStreamsQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var rows = await conn.QueryAsync<StreamDto>("""
            SELECT Id, Name, Description
            FROM AcademicStreams
            WHERE IsDeleted = 0
            ORDER BY Name
            """);

        return rows.AsList();
    }
}
