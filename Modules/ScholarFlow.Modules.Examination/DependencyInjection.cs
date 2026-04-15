using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ScholarFlow.Modules.Examination;

public static class DependencyInjection
{
    public static IServiceCollection AddExaminationModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
