using MediatR;

namespace ScholarFlow.Modules.Notifications.Commands.DeactivateNotificationDevice;

public sealed record DeactivateNotificationDeviceCommand(Guid DeviceId) : IRequest;
