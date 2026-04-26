using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ScholarFlow.Modules.UserProfiles.Commands.SetupStudentProfile;
using ScholarFlow.Modules.UserProfiles.EventHandlers;
using ScholarFlow.Modules.UserProfiles.Public;

namespace ScholarFlow.Modules.UserProfiles;

public static class DependencyInjection
{
    public static IServiceCollection AddUserProfilesModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                typeof(UserRegisteredIntegrationEventHandler).Assembly));

        services.AddScoped<IUserProfilesApi, UserProfilesApi>();

        return services;
    }
}