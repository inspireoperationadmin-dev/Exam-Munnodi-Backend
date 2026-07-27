using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Infrastructure.Persistence;
using ScholarFlow.Infrastructure.Persistence.Repositories;
using ScholarFlow.Infrastructure.Services;
using ScholarFlow.Infrastructure.Settings;
using ScholarFlow.SharedKernel.Behaviors;

namespace ScholarFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        // ── DbContext ─────────────────────────────────────────────────────────
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(
            sp => sp.GetRequiredService<ApplicationDbContext>());

        // ── ASP.NET Core Identity ─────────────────────────────────────────────
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit           = true;
            options.Password.RequireLowercase       = true;
            options.Password.RequireUppercase       = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength         = 8;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // ── JWT ───────────────────────────────────────────────────────────────
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.AddScoped<ITokenService, TokenService>();

        // ── Email (Resend) ────────────────────────────────────────────────────
        services.Configure<ResendSettings>(configuration.GetSection("Resend"));
        services.AddHttpClient("Resend", client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
        });
        services.AddScoped<IEmailService, ResendEmailService>();
        services.AddHostedService<OtpCleanupService>();
        services.AddHostedService<ExpiredExamSessionService>();

        // ── Current user (reads JWT claims from HttpContext) ──────────────────
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // ── ISqlConnectionFactory — for Dapper query handlers ─────────────────
        // Registered as singleton (stateless factory, creates connections per call).
        // Query handlers inject this directly and return flat read DTOs — NOT domain entities.
        services.AddSingleton<ISqlConnectionFactory>(
            _ => new SqlConnectionFactory(connectionString));

        // ── Repositories (EF Core, write-side + aggregate loading) ────────────
        // Scrutor scans and registers every Ef*Repository as its I*Repository interface.
        // Reads that need projections use ISqlConnectionFactory + Dapper in query handlers.
        services.Scan(scan => scan
            .FromAssemblyOf<ApplicationDbContext>()
            .AddClasses(c => c.InNamespaceOf<EfStudentProfileRepository>()
                              .Where(t => t.Name.StartsWith("Ef")))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // ── MediatR pipeline — registered once here for all modules ───────────
        // Registered as open generic transient — no assembly scan needed,
        // avoids MediatR's "no assemblies found" error when Infrastructure has no handlers.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
