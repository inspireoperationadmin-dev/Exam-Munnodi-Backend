using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Notifications.Commands.DeactivateNotificationDevice;

public sealed class DeactivateNotificationDeviceCommandHandler(
    ICurrentUser currentUser,
    INotificationDeviceRepository devices)
    : IRequestHandler<DeactivateNotificationDeviceCommand>
{
    public async Task Handle(DeactivateNotificationDeviceCommand request, CancellationToken ct)
    {
        var device = await devices.GetByIdAsync(request.DeviceId, ct)
            ?? throw new NotFoundException("Notification device not found.");

        if (device.UserId != currentUser.UserId)
            throw new ForbiddenException("You cannot change another user's notification device.");

        device.Deactivate();
        devices.Update(device);
        await devices.SaveChangesAsync(ct);
    }
}
