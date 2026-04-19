using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;

namespace ScholarFlow.Modules.UserProfiles.Public;

internal sealed class UserProfilesApi(IApplicationDbContext db) : IUserProfilesApi
{
    public Task<Guid?> GetTeacherProfileIdAsync(Guid userId, CancellationToken ct = default)
        => db.TeacherProfiles
            .Where(tp => tp.UserId == userId)
            .Select(tp => (Guid?)tp.Id)
            .FirstOrDefaultAsync(ct);

    public Task<bool> IsStudentConnectedToTeacherAsync(Guid studentUserId, Guid teacherProfileId, CancellationToken ct = default)
        => db.StudentTeacherConnections
            .AnyAsync(c =>
                c.StudentProfile.UserId == studentUserId
             && c.TeacherProfileId == teacherProfileId
             && c.Status == ConnectionStatus.Accepted,
            ct);
}
