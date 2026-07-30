using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Notifications.DTOs;

namespace ScholarFlow.Modules.Notifications.Queries.GetMyNotificationDevices;

public sealed class GetMyNotificationDevicesQueryHandler(
    ICurrentUser currentUser,
    ISqlConnectionFactory sql)
    : IRequestHandler<GetMyNotificationDevicesQuery, List<NotificationDeviceDto>>
{
    public async Task<List<NotificationDeviceDto>> Handle(GetMyNotificationDevicesQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var devices = await conn.QueryAsync<NotificationDeviceDto>("""
            SELECT
                Id,
                Platform,
                Provider,
                IsActive,
                LastSeenAt,
                EndpointHash,
                PushTokenHash,
                DeviceName
            FROM NotificationDevices
            WHERE UserId = @UserId
              AND IsDeleted = 0
            ORDER BY IsActive DESC, LastSeenAt DESC
            """, new { currentUser.UserId });

        return devices.ToList();
    }
}
