using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ScholarFlow.Modules.Identity.Commands.Register;

namespace ScholarFlow.Modules.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        // Register handlers — ValidationBehavior pipeline is registered once in AddInfrastructure()
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(RegisterCommand).Assembly));

        services.AddValidatorsFromAssembly(typeof(RegisterCommand).Assembly);

        return services;
    }
}
