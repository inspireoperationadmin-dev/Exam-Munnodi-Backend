using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.SharedKernel.IntegrationEvents;

namespace ScholarFlow.Modules.Notifications.EventHandlers;

public sealed class NotifyStudentSubscriptionActivatedHandler(
    IApplicationDbContext db,
    IWebPushNotificationSender sender,
    ILogger<NotifyStudentSubscriptionActivatedHandler> logger)
    : INotificationHandler<StudentSubscriptionActivatedIntegrationEvent>
{
    public async Task Handle(StudentSubscriptionActivatedIntegrationEvent notification, CancellationToken ct)
    {
        try
        {
            var devices = await db.NotificationDevices
                .AsNoTracking()
                .Where(device =>
                    device.UserId == notification.UserId &&
                    device.Platform == NotificationPlatform.WebPwa &&
                    device.Provider == NotificationProvider.WebPush &&
                    device.IsActive &&
                    device.Endpoint != null &&
                    device.P256dh != null &&
                    device.Auth != null)
                .ToListAsync(ct);

            var title = notification.IsScheduled
                ? "Subscription scheduled"
                : "Subscription activated";
            var body = notification.IsScheduled
                ? $"Your {notification.PlanName} plan is scheduled to start on {FormatDate(notification.StartsAt)}."
                : $"Your {notification.PlanName} plan is active until {FormatDate(notification.EndsAt)}.";

            foreach (var device in devices)
            {
                try
                {
                    await sender.SendAsync(
                        device.Endpoint!,
                        device.P256dh!,
                        device.Auth!,
                        title,
                        body,
                        "/subscription",
                        ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Failed to send subscription notification {EventId} to device {DeviceId}.",
                        notification.EventId,
                        device.Id);
                }
            }
        }
        catch (Exception ex)
        {
            // Subscription and payment are already committed; notification delivery is best-effort.
            logger.LogError(
                ex,
                "Failed to process subscription notification {EventId} for student {UserId}.",
                notification.EventId,
                notification.UserId);
        }
    }

    private static string FormatDate(DateTime value)
        => value.ToUniversalTime().ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
}
