namespace ScholarFlow.Domain.Interfaces;

public interface IWebPushNotificationSender
{
    Task SendAsync(
        string endpoint,
        string p256dh,
        string auth,
        string title,
        string body,
        string url,
        CancellationToken ct = default);
}
