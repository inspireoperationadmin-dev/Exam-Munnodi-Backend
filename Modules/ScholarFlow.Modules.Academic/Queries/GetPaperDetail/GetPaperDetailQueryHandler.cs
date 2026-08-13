using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Academic.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Queries.GetPaperDetail;

public sealed class GetPaperDetailQueryHandler(
    ISqlConnectionFactory sql,
    ICurrentUser currentUser)
    : IRequestHandler<GetPaperDetailQuery, PaperDetailDto>
{
    public async Task<PaperDetailDto> Handle(GetPaperDetailQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var dto = await conn.QueryFirstOrDefaultAsync<PaperDetailDto>("""
            SELECT
                p.Id,
                p.Title,
                p.SubjectId,
                CASE
                    WHEN sp.Medium = 2 THEN COALESCE(NULLIF(s.NameTamil, N''), s.NameEnglish)
                    WHEN sp.Medium = 1 THEN COALESCE(NULLIF(s.NameSinhala, N''), s.NameEnglish)
                    ELSE s.NameEnglish
                END AS SubjectName,
                p.Type,
                p.Medium,
                p.Year,
                p.Sitting,
                p.NegativeMarkValue,
                p.IsPublic,
                p.TimeLimit,
                p.OfficialPaperCode,
                (SELECT COUNT(*) FROM Questions q WHERE q.PaperId = p.Id AND q.IsDeleted = 0) AS QuestionCount,
                p.CreatedByTeacherId,
                p.CreatedAt
            FROM Papers p
            LEFT JOIN Subjects s ON s.Id = p.SubjectId AND s.IsDeleted = 0
            LEFT JOIN StudentProfiles sp ON sp.UserId = @UserId
            WHERE p.Id = @Id AND p.IsDeleted = 0
            """, new { request.Id, UserId = currentUser.UserId });

        return dto ?? throw new NotFoundException("Paper not found.");
    }
}
