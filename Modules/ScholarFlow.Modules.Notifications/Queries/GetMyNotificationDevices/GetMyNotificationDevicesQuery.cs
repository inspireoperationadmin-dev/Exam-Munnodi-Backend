using MediatR;
using ScholarFlow.Modules.Notifications.DTOs;

namespace ScholarFlow.Modules.Notifications.Queries.GetMyNotificationDevices;

public sealed record GetMyNotificationDevicesQuery : IRequest<List<NotificationDeviceDto>>;
