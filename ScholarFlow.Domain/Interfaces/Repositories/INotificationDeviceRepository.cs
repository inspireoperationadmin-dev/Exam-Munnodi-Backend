using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Interfaces.Repositories;

public interface INotificationDeviceRepository
{
    Task<NotificationDevice?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<NotificationDevice?> GetByWebEndpointAsync(string endpoint, CancellationToken ct = default);
    Task<NotificationDevice?> GetByNativeTokenAsync(
        NotificationPlatform platform,
        NotificationProvider provider,
        string pushToken,
        CancellationToken ct = default);

    Task AddAsync(NotificationDevice device, CancellationToken ct = default);
    void Update(NotificationDevice device);
    Task SaveChangesAsync(CancellationToken ct = default);
}
