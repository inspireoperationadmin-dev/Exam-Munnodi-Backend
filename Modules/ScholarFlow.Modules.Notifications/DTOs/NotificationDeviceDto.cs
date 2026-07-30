using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Modules.Notifications.DTOs;

public sealed record NotificationDeviceDto(
    Guid Id,
    NotificationPlatform Platform,
    NotificationProvider Provider,
    bool IsActive,
    DateTime LastSeenAt,
    string? EndpointHash,
    string? PushTokenHash,
    string? DeviceName);
