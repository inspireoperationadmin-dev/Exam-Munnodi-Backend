using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Interfaces;

namespace ScholarFlow.Modules.UserProfiles.Public;

internal sealed class UserProfilesApi(IApplicationDbContext db) : IUserProfilesApi
{
    public Task<Guid?> GetTeacherProfileIdAsync(Guid userId, CancellationToken ct = default)
        => db.TeacherProfiles
            .Where(tp => tp.UserId == userId)
            .Select(tp => (Guid?)tp.Id)
            .FirstOrDefaultAsync(ct);
}
