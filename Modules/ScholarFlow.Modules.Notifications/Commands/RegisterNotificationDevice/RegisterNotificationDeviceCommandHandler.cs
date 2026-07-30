using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.Modules.Notifications.DTOs;

namespace ScholarFlow.Modules.Notifications.Commands.RegisterNotificationDevice;

public sealed class RegisterNotificationDeviceCommandHandler(
    ICurrentUser currentUser,
    INotificationDeviceRepository devices)
    : IRequestHandler<RegisterNotificationDeviceCommand, NotificationDeviceDto>
{
    public async Task<NotificationDeviceDto> Handle(RegisterNotificationDeviceCommand request, CancellationToken ct)
    {
        NotificationDevice? device;

        if (request.Provider == NotificationProvider.WebPush)
        {
            device = await devices.GetByWebEndpointAsync(request.Endpoint!, ct);

            if (device is null)
            {
                device = NotificationDevice.CreateWebPush(
                    currentUser.UserId,
                    request.Endpoint!,
                    request.P256dh!,
                    request.Auth!,
                    request.DeviceName,
                    request.UserAgent,
                    request.AppVersion);

                await devices.AddAsync(device, ct);
            }
            else
            {
                device.AssignToUser(currentUser.UserId);
                device.RefreshWebPush(request.P256dh!, request.Auth!, request.DeviceName, request.UserAgent, request.AppVersion);
                devices.Update(device);
            }
        }
        else
        {
            device = await devices.GetByNativeTokenAsync(
                request.Platform,
                request.Provider,
                request.PushToken!,
                ct);

            if (device is null)
            {
                device = NotificationDevice.CreateNative(
                    currentUser.UserId,
                    request.Platform,
                    request.Provider,
                    request.PushToken!,
                    request.DeviceName,
                    request.UserAgent,
                    request.AppVersion);

                await devices.AddAsync(device, ct);
            }
            else
            {
                device.AssignToUser(currentUser.UserId);
                device.RefreshNative(request.PushToken!, request.DeviceName, request.UserAgent, request.AppVersion);
                devices.Update(device);
            }
        }

        await devices.SaveChangesAsync(ct);

        return new NotificationDeviceDto(
            device.Id,
            device.Platform,
            device.Provider,
            device.IsActive,
            device.LastSeenAt,
            device.EndpointHash,
            device.PushTokenHash,
            device.DeviceName);
    }
}
