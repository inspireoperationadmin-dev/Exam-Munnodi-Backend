using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.WebAPI.Infrastructure;

/// <summary>
/// Runs after authentication. Blocks access to protected routes for users
/// who have not completed email verification or profile setup.
/// Exempt routes: auth/*, and profile/setup endpoint.
/// </summary>
public sealed class OnboardingEnforcementMiddleware(RequestDelegate next)
{
    // These paths bypass the onboarding gate entirely
    private static readonly HashSet<string> ExemptPrefixes =
    [
        "/api/auth",                               // register, login, otp/send, otp/verify
        "/api/userprofiles/student/profile/setup", // profile setup itself
        "/api/academic/streams",                   // needed to render Setup form
        "/api/academic/subjects",                  // needed to render Setup form
        "/scalar",
        "/openapi"
    ];

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip anonymous routes and exempt paths
        if (!context.User.Identity?.IsAuthenticated is true ||
            ExemptPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var userIdStr = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Gate 1: email must be verified
        if (!user.EmailConfirmed)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                type    = "email_not_verified",
                message = "Please verify your email address before continuing."
            });
            return;
        }

        // Gate 2: profile must be set up
        if (!user.IsProfileSetup)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                type    = "profile_not_setup",
                message = "Please complete your profile setup before continuing."
            });
            return;
        }

        await next(context);
    }
}