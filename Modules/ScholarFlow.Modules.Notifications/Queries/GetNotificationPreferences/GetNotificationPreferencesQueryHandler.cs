using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Notifications.DTOs;

namespace ScholarFlow.Modules.Notifications.Queries.GetNotificationPreferences;

public sealed class GetNotificationPreferencesQueryHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser)
    : IRequestHandler<GetNotificationPreferencesQuery, NotificationPreferenceDto>
{
    public async Task<NotificationPreferenceDto> Handle(GetNotificationPreferencesQuery request, CancellationToken ct)
    {
        var preference = await db.StudentNotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == currentUser.UserId, ct);

        return preference is null
            ? ToDto(StudentNotificationPreference.CreateDefault(currentUser.UserId))
            : ToDto(preference);
    }

    private static NotificationPreferenceDto ToDto(StudentNotificationPreference preference)
        => new(
            preference.StudyRemindersEnabled,
            preference.DailyReminderTime.ToString(@"hh\:mm"),
            preference.TimeZoneId);
}
