using Dapper;
using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Academic.DTOs;

namespace ScholarFlow.Modules.Academic.Queries.GetPapers;

public sealed class GetPapersQueryHandler(
    ISqlConnectionFactory sql,
    ICurrentUser currentUser,
    ISubscriptionsApi subscriptionsApi)
    : IRequestHandler<GetPapersQuery, List<PaperSummaryDto>>
{
    public async Task<List<PaperSummaryDto>> Handle(GetPapersQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var where = new List<string> { "p.IsDeleted = 0" };
        if (currentUser.IsInRole(AppRole.Student)) where.Add("p.IsPublic = 1");
        if (request.SubjectId.HasValue) where.Add("p.SubjectId = @SubjectId");
        if (request.Type.HasValue)      where.Add("p.Type = @Type");
        if (request.Medium.HasValue)    where.Add("p.Medium = @Medium");
        if (request.Year.HasValue)      where.Add("p.Year = @Year");

        var sql_ = $"""
            SELECT
                p.Id,
                p.Title,
                ISNULL(
                    CASE
                        WHEN sp.Medium = 2 THEN COALESCE(NULLIF(s.NameTamil, N''), s.NameEnglish)
                        WHEN sp.Medium = 1 THEN COALESCE(NULLIF(s.NameSinhala, N''), s.NameEnglish)
                        ELSE s.NameEnglish
                    END,
                    ''
                ) AS SubjectName,
                p.Type,
                p.Medium,
                p.Year,
                p.Sitting,
                (SELECT COUNT(*) FROM Questions q WHERE q.PaperId = p.Id AND q.IsDeleted = 0) AS QuestionCount,
                p.IsPublic,
                p.TimeLimit,
                p.CreatedAt
            FROM Papers p
            LEFT JOIN Subjects s ON s.Id = p.SubjectId AND s.IsDeleted = 0
            LEFT JOIN StudentProfiles sp ON sp.UserId = @UserId
            WHERE {string.Join(" AND ", where)}
            ORDER BY p.Year DESC, p.Title
            """;

        var rows = await conn.QueryAsync<PaperSummaryDto>(sql_, new
        {
            UserId = currentUser.UserId,
            request.SubjectId,
            Type   = request.Type?.ToString(),
            Medium = request.Medium?.ToString(),
            request.Year
        });

        var paperList = rows.AsList();
        if (!currentUser.IsInRole(AppRole.Student))
        {
            return paperList
                .Select(p => p with { CanPractice = true, CanUseExamMode = true })
                .ToList();
        }

        var accessByPaperId = await subscriptionsApi.GetPaperAccessAsync(
            currentUser.UserId,
            paperList.Select(p => p.Id).ToList(),
            ct);

        return paperList.Select(p =>
        {
            var access = accessByPaperId[p.Id];
            return p with
            {
                IsLocked = access.IsLocked,
                CanPractice = access.CanPractice,
                CanUseExamMode = access.CanUseExamMode,
                LockReason = access.LockReason,
                RequiredPlan = access.RequiredPlan?.ToString()
            };
        }).ToList();
    }
}
