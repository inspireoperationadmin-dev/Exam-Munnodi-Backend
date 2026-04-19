namespace ScholarFlow.Modules.UserProfiles.Public;

/// <summary>
/// Public API exposed by the UserProfiles module.
/// Other modules inject this interface — never access UserProfiles DbSets directly.
/// </summary>
public interface IUserProfilesApi
{
    /// <summary>Returns the TeacherProfile.Id for the given userId, or null if not found.</summary>
    Task<Guid?> GetTeacherProfileIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns true if the student (by userId) has an Accepted connection to the given TeacherProfile.
    /// Used by Examination module to check private paper access.
    /// </summary>
    Task<bool> IsStudentConnectedToTeacherAsync(Guid studentUserId, Guid teacherProfileId, CancellationToken ct = default);
}
