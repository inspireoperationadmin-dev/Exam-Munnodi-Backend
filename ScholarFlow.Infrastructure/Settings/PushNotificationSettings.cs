namespace ScholarFlow.Infrastructure.Settings;

public sealed class PushNotificationSettings
{
    public string? VapidPublicKey { get; init; }
    public string? VapidPrivateKey { get; init; }
    public string? VapidSubject { get; init; }
}
