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
        await SeedSubscriptionPlansAsync(db);
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

    private static async Task SeedSubscriptionPlansAsync(ApplicationDbContext db)
    {
        var plans = new[]
        {
            new PlanSeed(SubscriptionPlanCode.Free, "Free", SubscriptionTier.Free, SubscriptionBillingCycle.Free,
                0, 0m, 0m, false, 2, 2, 5, 3, ProgressAccessLevel.Overall, 1),
            new PlanSeed(SubscriptionPlanCode.BasicMonthly, "Basic Monthly", SubscriptionTier.Basic, SubscriptionBillingCycle.Monthly,
                30, 490m, 0m, true, null, null, 15, 20, ProgressAccessLevel.Detailed, 2),
            new PlanSeed(SubscriptionPlanCode.BasicAnnual, "Basic Annual", SubscriptionTier.Basic, SubscriptionBillingCycle.Annual,
                365, 5880m, 30m, true, null, null, 15, 20, ProgressAccessLevel.Detailed, 3),
            new PlanSeed(SubscriptionPlanCode.ProMonthly, "Pro Monthly", SubscriptionTier.Pro, SubscriptionBillingCycle.Monthly,
                30, 890m, 0m, true, null, null, null, null, ProgressAccessLevel.Full, 4),
            new PlanSeed(SubscriptionPlanCode.ProAnnual, "Pro Annual", SubscriptionTier.Pro, SubscriptionBillingCycle.Annual,
                365, 10680m, 30m, true, null, null, null, null, ProgressAccessLevel.Full, 5)
        };

        foreach (var seed in plans)
        {
            var existing = await db.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Code == seed.Code);

            if (existing is null)
            {
                await db.SubscriptionPlans.AddAsync(SubscriptionPlan.Create(
                    seed.Code,
                    seed.Name,
                    seed.Tier,
                    seed.BillingCycle,
                    seed.DurationDays,
                    seed.BasePriceLkr,
                    seed.DiscountPercentage,
                    seed.AllowsPaperExamMode,
                    seed.FreePastPaperCount,
                    seed.FreeModelPaperCount,
                    seed.MonthlyMockExamLimit,
                    seed.MonthlyUnitExamLimit,
                    seed.ProgressAccessLevel,
                    seed.SortOrder));
            }
            else
            {
                existing.Update(
                    seed.Name,
                    seed.Tier,
                    seed.BillingCycle,
                    seed.DurationDays,
                    seed.AllowsPaperExamMode,
                    seed.FreePastPaperCount,
                    seed.FreeModelPaperCount,
                    seed.MonthlyMockExamLimit,
                    seed.MonthlyUnitExamLimit,
                    seed.ProgressAccessLevel,
                    isActive: true,
                    seed.SortOrder);
            }
        }

        var legacyCodes = new[]
        {
            SubscriptionPlanCode.LaunchFree,
            SubscriptionPlanCode.Trial,
            SubscriptionPlanCode.Monthly,
            SubscriptionPlanCode.Quarterly,
            SubscriptionPlanCode.SixMonths
        };

        var legacyPlans = await db.SubscriptionPlans
            .Where(p => legacyCodes.Contains(p.Code))
            .ToListAsync();

        foreach (var legacyPlan in legacyPlans)
            legacyPlan.Deactivate();

        await db.SaveChangesAsync();
    }

    private sealed record PlanSeed(
        SubscriptionPlanCode Code,
        string Name,
        SubscriptionTier Tier,
        SubscriptionBillingCycle BillingCycle,
        int DurationDays,
        decimal BasePriceLkr,
        decimal DiscountPercentage,
        bool AllowsPaperExamMode,
        int? FreePastPaperCount,
        int? FreeModelPaperCount,
        int? MonthlyMockExamLimit,
        int? MonthlyUnitExamLimit,
        ProgressAccessLevel ProgressAccessLevel,
        int SortOrder);
}
