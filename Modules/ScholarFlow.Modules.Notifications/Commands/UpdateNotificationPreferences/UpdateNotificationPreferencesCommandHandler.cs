using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Notifications.DTOs;

namespace ScholarFlow.Modules.Notifications.Commands.UpdateNotificationPreferences;

public sealed class UpdateNotificationPreferencesCommandHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateNotificationPreferencesCommand, NotificationPreferenceDto>
{
    public async Task<NotificationPreferenceDto> Handle(
        UpdateNotificationPreferencesCommand request,
        CancellationToken ct)
    {
        var preference = await db.StudentNotificationPreferences
            .FirstOrDefaultAsync(item => item.UserId == currentUser.UserId, ct);

        if (preference is null)
        {
            preference = StudentNotificationPreference.CreateDefault(currentUser.UserId);
            db.StudentNotificationPreferences.Add(preference);
        }

        preference.Update(
            request.StudyRemindersEnabled,
            TimeSpan.Parse(request.DailyReminderTime),
            request.TimeZoneId);

        await db.SaveChangesAsync(ct);

        return new NotificationPreferenceDto(
            preference.StudyRemindersEnabled,
            preference.DailyReminderTime.ToString(@"hh\:mm"),
            preference.TimeZoneId);
    }
}
