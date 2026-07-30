using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Infrastructure.Persistence;

namespace ScholarFlow.Infrastructure.Services;

public sealed class StudyReminderService(
    IServiceScopeFactory scopeFactory,
    ILogger<StudyReminderService> logger)
    : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendDueRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Study reminder processing failed.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task SendDueRemindersAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IWebPushNotificationSender>();
        var utcNow = DateTime.UtcNow;

        var preferences = await db.StudentNotificationPreferences
            .Where(preference => preference.StudyRemindersEnabled)
            .ToListAsync(ct);

        foreach (var preference in preferences)
        {
            var timeZone = ResolveTimeZone(preference.TimeZoneId);
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
            var localToday = localNow.Date;

            if (localNow.TimeOfDay < preference.DailyReminderTime)
            {
                continue;
            }

            if (WasReminderSentToday(preference, timeZone, localToday))
            {
                continue;
            }

            var studiedToday = await db.StudentStudyActivities
                .AnyAsync(activity =>
                    activity.UserId == preference.UserId &&
                    activity.ActivityDate == localToday,
                    ct);

            if (studiedToday)
            {
                continue;
            }

            var devices = await db.NotificationDevices
                .Where(device =>
                    device.UserId == preference.UserId &&
                    device.Platform == NotificationPlatform.WebPwa &&
                    device.Provider == NotificationProvider.WebPush &&
                    device.IsActive &&
                    device.Endpoint != null &&
                    device.P256dh != null &&
                    device.Auth != null)
                .ToListAsync(ct);

            var sent = 0;
            foreach (var device in devices)
            {
                try
                {
                    await sender.SendAsync(
                        device.Endpoint!,
                        device.P256dh!,
                        device.Auth!,
                        "Exam Munnodi",
                        GetReminderBody(),
                        "/",
                        ct);

                    sent++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send study reminder to device {DeviceId}.", device.Id);
                }
            }

            if (sent > 0)
            {
                preference.MarkDailyReminderSent(utcNow);
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private static string GetReminderBody()
        => "Your study session is ready.";

    private static bool WasReminderSentToday(
        StudentNotificationPreference preference,
        TimeZoneInfo timeZone,
        DateTime localToday)
    {
        if (!preference.LastDailyReminderSentAt.HasValue)
        {
            return false;
        }

        var sentLocal = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(preference.LastDailyReminderSentAt.Value, DateTimeKind.Utc),
            timeZone);

        return sentLocal.Date == localToday;
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        foreach (var candidate in new[] { timeZoneId, "Asia/Colombo", "Sri Lanka Standard Time", "UTC" })
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
