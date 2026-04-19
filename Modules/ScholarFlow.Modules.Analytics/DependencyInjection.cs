using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Analytics.Public;
using ScholarFlow.Modules.Analytics.Queries.GetSubjectPerformance;

namespace ScholarFlow.Modules.Analytics;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetSubjectPerformanceQuery).Assembly));

        // Module Public APIs — defined in Domain to avoid circular references
        services.AddScoped<IAnalyticsApi, AnalyticsApi>();

        return services;
    }
}
