using ScholarFlow.Domain.Entities.Base;

namespace ScholarFlow.Domain.Entities;

public class StudentNotificationPreference : AuditableEntity
{
    private StudentNotificationPreference() { }

    public Guid UserId { get; private set; }
    public bool StudyRemindersEnabled { get; private set; }
    public bool StreakRemindersEnabled { get; private set; }
    public TimeSpan DailyReminderTime { get; private set; }
    public string TimeZoneId { get; private set; } = DefaultTimeZoneId;
    public DateTime? LastDailyReminderSentAt { get; private set; }

    public ApplicationUser User { get; set; } = null!;

    public const string DefaultTimeZoneId = "Asia/Colombo";
    public static readonly TimeSpan DefaultDailyReminderTime = new(18, 30, 0);

    public static StudentNotificationPreference CreateDefault(Guid userId)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StudyRemindersEnabled = true,
            StreakRemindersEnabled = true,
            DailyReminderTime = DefaultDailyReminderTime,
            TimeZoneId = DefaultTimeZoneId
        };

    public void Update(
        bool studyRemindersEnabled,
        TimeSpan dailyReminderTime,
        string? timeZoneId)
    {
        StudyRemindersEnabled = studyRemindersEnabled;
        StreakRemindersEnabled = true;
        DailyReminderTime = dailyReminderTime;
        TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId)
            ? DefaultTimeZoneId
            : timeZoneId.Trim();
    }

    public void MarkDailyReminderSent(DateTime utcNow)
    {
        LastDailyReminderSentAt = utcNow;
    }
}
