using System.Text.Json;
using Microsoft.Extensions.Options;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Infrastructure.Settings;
using ScholarFlow.SharedKernel.Exceptions;
using WebPush;

namespace ScholarFlow.Infrastructure.Services;

public sealed class WebPushNotificationSender(IOptions<PushNotificationSettings> settings)
    : IWebPushNotificationSender
{
    public async Task SendAsync(
        string endpoint,
        string p256dh,
        string auth,
        string title,
        string body,
        string url,
        CancellationToken ct = default)
    {
        var pushSettings = settings.Value;

        if (string.IsNullOrWhiteSpace(pushSettings.VapidPublicKey)
            || string.IsNullOrWhiteSpace(pushSettings.VapidPrivateKey)
            || string.IsNullOrWhiteSpace(pushSettings.VapidSubject))
        {
            throw new BadRequestException("Web push notifications are not configured.");
        }

        var subscription = new PushSubscription(endpoint, p256dh, auth);
        var vapid = new VapidDetails(
            pushSettings.VapidSubject,
            pushSettings.VapidPublicKey,
            pushSettings.VapidPrivateKey);

        var payload = JsonSerializer.Serialize(new
        {
            title,
            body,
            url
        });

        using var client = new WebPushClient();
        await client.SendNotificationAsync(subscription, payload, vapid, ct);
    }
}
