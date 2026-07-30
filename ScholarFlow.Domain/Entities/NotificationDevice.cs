using ScholarFlow.Domain.Entities.Base;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace ScholarFlow.Domain.Entities;

public class NotificationDevice : AuditableEntity
{
    private NotificationDevice() { }

    public Guid UserId { get; private set; }
    public NotificationPlatform Platform { get; private set; }
    public NotificationProvider Provider { get; private set; }
    public string? Endpoint { get; private set; }
    public string? EndpointHash { get; private set; }
    public string? P256dh { get; private set; }
    public string? Auth { get; private set; }
    public string? PushToken { get; private set; }
    public string? PushTokenHash { get; private set; }
    public string? DeviceName { get; private set; }
    public string? UserAgent { get; private set; }
    public string? AppVersion { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime LastSeenAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public ApplicationUser User { get; set; } = null!;

    public static NotificationDevice CreateWebPush(
        Guid userId,
        string endpoint,
        string p256dh,
        string auth,
        string? deviceName,
        string? userAgent,
        string? appVersion)
    {
        if (string.IsNullOrWhiteSpace(endpoint)) throw new DomainException("Web push endpoint is required.");
        if (string.IsNullOrWhiteSpace(p256dh)) throw new DomainException("Web push p256dh key is required.");
        if (string.IsNullOrWhiteSpace(auth)) throw new DomainException("Web push auth key is required.");

        return new NotificationDevice
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Platform = NotificationPlatform.WebPwa,
            Provider = NotificationProvider.WebPush,
            Endpoint = endpoint.Trim(),
            EndpointHash = Hash(endpoint),
            P256dh = p256dh.Trim(),
            Auth = auth.Trim(),
            DeviceName = Normalize(deviceName),
            UserAgent = Normalize(userAgent),
            AppVersion = Normalize(appVersion),
            IsActive = true,
            LastSeenAt = DateTime.UtcNow
        };
    }

    public static NotificationDevice CreateNative(
        Guid userId,
        NotificationPlatform platform,
        NotificationProvider provider,
        string pushToken,
        string? deviceName,
        string? userAgent,
        string? appVersion)
    {
        if (platform is not (NotificationPlatform.Android or NotificationPlatform.Ios))
            throw new DomainException("Native notification device must use Android or iOS platform.");

        if (provider is not (NotificationProvider.Fcm or NotificationProvider.Apns))
            throw new DomainException("Native notification device must use FCM or APNS provider.");

        if (string.IsNullOrWhiteSpace(pushToken)) throw new DomainException("Native push token is required.");

        return new NotificationDevice
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Platform = platform,
            Provider = provider,
            PushToken = pushToken.Trim(),
            PushTokenHash = Hash(pushToken),
            DeviceName = Normalize(deviceName),
            UserAgent = Normalize(userAgent),
            AppVersion = Normalize(appVersion),
            IsActive = true,
            LastSeenAt = DateTime.UtcNow
        };
    }

    public void RefreshWebPush(string p256dh, string auth, string? deviceName, string? userAgent, string? appVersion)
    {
        if (Provider != NotificationProvider.WebPush)
            throw new DomainException("Only web push devices can refresh web push keys.");

        P256dh = p256dh.Trim();
        Auth = auth.Trim();
        EndpointHash = Hash(Endpoint);
        RefreshMetadata(deviceName, userAgent, appVersion);
    }

    public void RefreshNative(string pushToken, string? deviceName, string? userAgent, string? appVersion)
    {
        if (Provider is not (NotificationProvider.Fcm or NotificationProvider.Apns))
            throw new DomainException("Only native notification devices can refresh native push tokens.");

        PushToken = pushToken.Trim();
        PushTokenHash = Hash(pushToken);
        RefreshMetadata(deviceName, userAgent, appVersion);
    }

    public void AssignToUser(Guid userId)
    {
        UserId = userId;
    }

    public void Deactivate()
    {
        IsActive = false;
        RevokedAt = DateTime.UtcNow;
    }

    private void RefreshMetadata(string? deviceName, string? userAgent, string? appVersion)
    {
        DeviceName = Normalize(deviceName);
        UserAgent = Normalize(userAgent);
        AppVersion = Normalize(appVersion);
        IsActive = true;
        RevokedAt = null;
        LastSeenAt = DateTime.UtcNow;
    }

    private static string? Normalize(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    public static string Hash(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
        return Convert.ToHexString(bytes);
    }
}
