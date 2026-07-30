namespace ScholarFlow.Modules.Notifications.DTOs;

public sealed record NotificationPreferenceDto(
    bool StudyRemindersEnabled,
    string DailyReminderTime,
    string TimeZoneId);
