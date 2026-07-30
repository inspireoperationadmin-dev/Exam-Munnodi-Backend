namespace ScholarFlow.Modules.Notifications.DTOs;

public sealed record VapidPublicKeyDto(
    bool IsConfigured,
    string? PublicKey);
