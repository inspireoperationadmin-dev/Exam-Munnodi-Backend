using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.Notifications.DTOs;

namespace ScholarFlow.Modules.Notifications.Commands.RegisterNotificationDevice;

public sealed record RegisterNotificationDeviceCommand(
    NotificationPlatform Platform,
    NotificationProvider Provider,
    string? Endpoint,
    string? P256dh,
    string? Auth,
    string? PushToken,
    string? DeviceName,
    string? UserAgent,
    string? AppVersion) : IRequest<NotificationDeviceDto>;
