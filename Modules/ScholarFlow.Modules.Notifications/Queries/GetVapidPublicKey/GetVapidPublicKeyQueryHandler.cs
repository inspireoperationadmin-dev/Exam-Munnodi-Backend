using MediatR;
using Microsoft.Extensions.Options;
using ScholarFlow.Infrastructure.Settings;
using ScholarFlow.Modules.Notifications.DTOs;

namespace ScholarFlow.Modules.Notifications.Queries.GetVapidPublicKey;

public sealed class GetVapidPublicKeyQueryHandler(IOptions<PushNotificationSettings> settings)
    : IRequestHandler<GetVapidPublicKeyQuery, VapidPublicKeyDto>
{
    public Task<VapidPublicKeyDto> Handle(GetVapidPublicKeyQuery request, CancellationToken ct)
    {
        var publicKey = settings.Value.VapidPublicKey;
        var isConfigured = !string.IsNullOrWhiteSpace(publicKey)
            && !string.IsNullOrWhiteSpace(settings.Value.VapidPrivateKey)
            && !string.IsNullOrWhiteSpace(settings.Value.VapidSubject);

        return Task.FromResult(new VapidPublicKeyDto(isConfigured, publicKey));
    }
}
