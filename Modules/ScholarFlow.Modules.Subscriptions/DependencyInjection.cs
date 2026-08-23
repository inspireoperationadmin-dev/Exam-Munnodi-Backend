using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Subscriptions.Commands.ActivateStudentSubscription;
using ScholarFlow.Modules.Subscriptions.Public;
using ScholarFlow.Modules.Subscriptions.Settings;

namespace ScholarFlow.Modules.Subscriptions;

public static class DependencyInjection
{
    public static IServiceCollection AddSubscriptionsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ActivateStudentSubscriptionCommand).Assembly));

        services.AddValidatorsFromAssembly(typeof(ActivateStudentSubscriptionCommand).Assembly);
        services.AddScoped<ISubscriptionsApi, SubscriptionsApi>();
        services.Configure<LaunchOfferSettings>(
            configuration.GetSection(LaunchOfferSettings.SectionName));

        return services;
    }
}
