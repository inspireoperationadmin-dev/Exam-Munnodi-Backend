using MediatR;
using ScholarFlow.Modules.Notifications.DTOs;

namespace ScholarFlow.Modules.Notifications.Commands.UpdateNotificationPreferences;

public sealed record UpdateNotificationPreferencesCommand(
    bool StudyRemindersEnabled,
    string DailyReminderTime,
    string? TimeZoneId) : IRequest<NotificationPreferenceDto>;
