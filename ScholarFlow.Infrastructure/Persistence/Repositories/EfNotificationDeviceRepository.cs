using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces.Repositories;

namespace ScholarFlow.Infrastructure.Persistence.Repositories;

public sealed class EfNotificationDeviceRepository(ApplicationDbContext db) : INotificationDeviceRepository
{
    public Task<NotificationDevice?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.NotificationDevices.FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<NotificationDevice?> GetByWebEndpointAsync(string endpoint, CancellationToken ct = default)
        => db.NotificationDevices.FirstOrDefaultAsync(
            d => d.Provider == NotificationProvider.WebPush
              && d.EndpointHash == NotificationDevice.Hash(endpoint),
            ct);

    public Task<NotificationDevice?> GetByNativeTokenAsync(
        NotificationPlatform platform,
        NotificationProvider provider,
        string pushToken,
        CancellationToken ct = default)
        => db.NotificationDevices.FirstOrDefaultAsync(
            d => d.Platform == platform
              && d.Provider == provider
              && d.PushTokenHash == NotificationDevice.Hash(pushToken),
            ct);

    public async Task AddAsync(NotificationDevice device, CancellationToken ct = default)
        => await db.NotificationDevices.AddAsync(device, ct);

    public void Update(NotificationDevice device)
        => db.NotificationDevices.Update(device);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
