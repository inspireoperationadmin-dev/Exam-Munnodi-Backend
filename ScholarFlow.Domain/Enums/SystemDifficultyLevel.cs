namespace ScholarFlow.Domain.Enums;

/// <summary>
/// System-calculated difficulty based on student performance data.
/// Updated automatically after each exam session via Analytics module.
/// Requires minimum 5 attempts before being set.
/// </summary>
public enum SystemDifficultyLevel
{
    /// <summary>CorrectRate > 70% across all students.</summary>
    Easy   = 1,

    /// <summary>CorrectRate 40–70% across all students.</summary>
    Medium = 2,

    /// <summary>CorrectRate < 40% across all students.</summary>
    Hard   = 3
}
