using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Examination.Commands.StartExamSession;
using ScholarFlow.Modules.Examination.Public;

namespace ScholarFlow.Modules.Examination;

public static class DependencyInjection
{
    public static IServiceCollection AddExaminationModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(StartExamSessionCommand).Assembly));

        services.AddValidatorsFromAssembly(typeof(StartExamSessionCommand).Assembly);

        // Module Public APIs — defined in Domain to avoid circular references
        services.AddScoped<IExaminationApi, ExaminationApi>();

        return services;
    }
}
