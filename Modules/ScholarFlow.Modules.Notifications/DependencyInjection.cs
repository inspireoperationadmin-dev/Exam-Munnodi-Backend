using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ScholarFlow.Modules.Notifications.Commands.RegisterNotificationDevice;

namespace ScholarFlow.Modules.Notifications;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(RegisterNotificationDeviceCommand).Assembly));

        services.AddValidatorsFromAssembly(typeof(RegisterNotificationDeviceCommand).Assembly);

        return services;
    }
}
