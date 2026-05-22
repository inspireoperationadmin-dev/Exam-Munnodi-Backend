using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Infrastructure.Persistence;

/// <summary>
/// Seeds essential reference data on first startup.
/// Safe to call on every startup — all operations are idempotent.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        // ── Run migrations first ──────────────────────────────────────────────
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedRolesAsync(roleManager);
        await SeedSuperAdminAsync(userManager);
    }

    // ── Roles ─────────────────────────────────────────────────────────────────

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        string[] roles = [AppRole.SuperAdmin, AppRole.Admin, AppRole.Teacher, AppRole.Student];

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (!result.Succeeded)
                    throw new Exception($"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }

    // ── SuperAdmin ────────────────────────────────────────────────────────────

    private static async Task SeedSuperAdminAsync(UserManager<ApplicationUser> userManager)
    {
        const string email    = "superadmin@scholarflow.lk";
        const string password = "Admin@123456";

        if (await userManager.FindByEmailAsync(email) is not null)
            return; // Already seeded

        var superAdmin = new ApplicationUser
        {
            Id                 = Guid.Parse("44444444-0000-0000-0000-000000000001"),
            UserName           = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email              = email,
            NormalizedEmail    = email.ToUpperInvariant(),
            EmailConfirmed     = true,
            SecurityStamp      = Guid.NewGuid().ToString()
        };

        var createResult = await userManager.CreateAsync(superAdmin, password);
        if (!createResult.Succeeded)
            throw new Exception($"Failed to create SuperAdmin: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");

        var roleResult = await userManager.AddToRoleAsync(superAdmin, AppRole.SuperAdmin);
        if (!roleResult.Succeeded)
            throw new Exception($"Failed to assign SuperAdmin role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
    }
}