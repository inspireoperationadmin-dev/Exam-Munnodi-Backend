using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ScholarFlow.Modules.Academic.Commands.Streams.CreateStream;
using ScholarFlow.Modules.Academic.Public;

namespace ScholarFlow.Modules.Academic;

public static class DependencyInjection
{
    public static IServiceCollection AddAcademicModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreateStreamCommand).Assembly));

        services.AddValidatorsFromAssembly(typeof(CreateStreamCommand).Assembly);

        // Module Public API — Examination module injects IAcademicApi, never Academic DbSets directly
        services.AddScoped<IAcademicApi, AcademicApi>();

        return services;
    }
}
