using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ScholarFlow.Modules.Analytics;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
