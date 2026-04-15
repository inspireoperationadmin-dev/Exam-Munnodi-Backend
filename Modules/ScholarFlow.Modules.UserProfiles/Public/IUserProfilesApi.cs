namespace ScholarFlow.Modules.UserProfiles.Public;

/// <summary>
/// Public API exposed by the UserProfiles module.
/// Other modules inject this interface — never access UserProfiles DbSets directly.
/// </summary>
public interface IUserProfilesApi
{
    /// <summary>Returns the TeacherProfile.Id for the given userId, or null if not found.</summary>
    Task<Guid?> GetTeacherProfileIdAsync(Guid userId, CancellationToken ct = default);
}
