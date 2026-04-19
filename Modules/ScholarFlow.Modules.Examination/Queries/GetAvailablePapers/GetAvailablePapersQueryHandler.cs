using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Queries.GetAvailablePapers;

public sealed class GetAvailablePapersQueryHandler(
    ISqlConnectionFactory sql,
    ICurrentUser currentUser)
    : IRequestHandler<GetAvailablePapersQuery, List<AvailablePaperDto>>
{
    public async Task<List<AvailablePaperDto>> Handle(GetAvailablePapersQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var rows = await conn.QueryAsync<AvailablePaperRow>("""
            SELECT
                p.Id,
                p.Title,
                p.SubjectId,
                s.Name          AS SubjectName,
                p.Year,
                p.Type,
                p.Medium,
                p.Sitting,
                p.OfficialPaperCode,
                p.NegativeMarkValue,
                COUNT(q.Id)     AS QuestionCount
            FROM Papers p
            LEFT JOIN Subjects s    ON s.Id = p.SubjectId AND s.IsDeleted = 0
            LEFT JOIN Questions q   ON q.PaperId = p.Id AND q.IsDeleted = 0
            LEFT JOIN TeacherProfiles tp ON tp.Id = p.CreatedByTeacherId
            LEFT JOIN StudentProfiles sp ON sp.UserId = @UserId
            LEFT JOIN StudentTeacherConnections stc
                ON stc.TeacherProfileId = tp.Id
               AND stc.StudentProfileId = sp.Id
               AND stc.Status = 'Accepted'
            WHERE p.IsDeleted = 0
              AND (p.IsPublic = 1 OR stc.Id IS NOT NULL)
              AND (@SubjectId IS NULL OR p.SubjectId = @SubjectId)
              AND (@Type     IS NULL OR p.Type      = @Type)
              AND (@Medium   IS NULL OR p.Medium    = @Medium)
              AND (@Year     IS NULL OR p.Year      = @Year)
            GROUP BY
                p.Id, p.Title, p.SubjectId, s.Name,
                p.Year, p.Type, p.Medium, p.Sitting,
                p.OfficialPaperCode, p.NegativeMarkValue
            ORDER BY p.Year DESC, p.Title
            """,
            new
            {
                UserId    = currentUser.UserId,
                request.SubjectId,
                request.Type,
                request.Medium,
                request.Year
            });

        return rows.Select(r => new AvailablePaperDto(
            Id:                r.Id,
            Title:             r.Title,
            SubjectId:         r.SubjectId,
            SubjectName:       r.SubjectName,
            Year:              r.Year,
            Type:              r.Type,
            Medium:            r.Medium,
            Sitting:           r.Sitting,
            OfficialPaperCode: r.OfficialPaperCode,
            NegativeMarkValue: r.NegativeMarkValue,
            QuestionCount:     r.QuestionCount))
            .ToList();
    }

    private sealed record AvailablePaperRow(
        Guid Id,
        string Title,
        Guid? SubjectId,
        string? SubjectName,
        int Year,
        string Type,
        string Medium,
        string? Sitting,
        string? OfficialPaperCode,
        decimal NegativeMarkValue,
        int QuestionCount);
}
