using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces.Repositories;

namespace ScholarFlow.Infrastructure.Persistence.Repositories;

public sealed class EfSubscriptionRepository(ApplicationDbContext db) : ISubscriptionRepository
{
    public Task<SubscriptionPlan?> GetPlanByCodeAsync(SubscriptionPlanCode code, CancellationToken ct = default)
        => db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Code == code, ct);

    public Task<StudentSubscription?> GetCurrentActiveAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        return db.StudentSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId
                     && s.Status == SubscriptionStatus.Active
                     && s.StartsAt <= now
                     && s.EndsAt > now
                     && s.Plan.IsActive
                     && s.Plan.Tier != SubscriptionTier.Free)
            .OrderByDescending(s => s.EndsAt)
            .FirstOrDefaultAsync(ct);
    }

    public Task<StudentSubscription?> GetNextScheduledAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        return db.StudentSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId
                     && s.Status == SubscriptionStatus.Active
                     && s.StartsAt > now
                     && s.EndsAt > s.StartsAt
                     && s.Plan.IsActive
                     && s.Plan.Tier != SubscriptionTier.Free)
            .OrderBy(s => s.StartsAt)
            .ThenBy(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<DateTime?> GetActivePlanAccessEndAsync(
        Guid userId,
        Guid planId,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var periods = await db.StudentSubscriptions
            .Where(s => s.UserId == userId
                     && s.PlanId == planId
                     && s.Status == SubscriptionStatus.Active
                     && s.EndsAt > now)
            .OrderBy(s => s.StartsAt)
            .Select(s => new { s.StartsAt, s.EndsAt })
            .ToListAsync(ct);

        var accessEnd = periods
            .Where(period => period.StartsAt <= now && period.EndsAt > now)
            .Select(period => (DateTime?)period.EndsAt)
            .Max();

        if (!accessEnd.HasValue) return null;

        foreach (var period in periods.Where(period => period.StartsAt > now))
        {
            if (period.StartsAt > accessEnd.Value) break;
            if (period.EndsAt > accessEnd.Value) accessEnd = period.EndsAt;
        }

        return accessEnd;
    }

    public Task<StudentSubscription?> GetSubscriptionAsync(
        Guid subscriptionId,
        CancellationToken ct = default)
        => db.StudentSubscriptions.SingleOrDefaultAsync(s => s.Id == subscriptionId, ct);

    public async Task<IReadOnlyList<StudentSubscription>> GetActiveSubscriptionsAsync(Guid userId, CancellationToken ct = default)
        => await db.StudentSubscriptions
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.EndsAt)
            .ToListAsync(ct);

    public Task<bool> HasPromotionAsync(Guid userId, string promotionCode, CancellationToken ct = default)
        => db.StudentSubscriptions.AnyAsync(
            s => s.UserId == userId && s.PromotionCode == promotionCode,
            ct);

    public async Task<bool> TryAddPromotionSubscriptionAsync(
        StudentSubscription subscription,
        CancellationToken ct = default)
    {
        await db.StudentSubscriptions.AddAsync(subscription, ct);

        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            db.Entry(subscription).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<int> GetUsageAsync(
        Guid userId,
        DateTime periodStart,
        SubscriptionUsageFeature feature,
        CancellationToken ct = default)
        => await db.StudentSubscriptionUsages
            .Where(u => u.UserId == userId
                     && u.PeriodStart == periodStart.Date
                     && u.Feature == feature)
            .Select(u => u.UsedCount)
            .SingleOrDefaultAsync(ct);

    public async Task<bool> TryConsumeUsageAsync(
        Guid userId,
        DateTime periodStart,
        SubscriptionUsageFeature feature,
        int limit,
        CancellationToken ct = default)
    {
        if (limit <= 0) return false;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                return await TryConsumeUsageOnceAsync(
                    userId,
                    periodStart.Date,
                    feature,
                    limit,
                    ct);
            }
            catch (DbUpdateException ex) when (
                attempt < 3
                && ex.InnerException is SqlException { Number: 1205 or 2601 or 2627 })
            {
                db.ChangeTracker.Clear();
                await Task.Delay(attempt * 25, ct);
            }
            catch (SqlException ex) when (attempt < 3 && ex.Number == 1205)
            {
                db.ChangeTracker.Clear();
                await Task.Delay(attempt * 25, ct);
            }
        }

        throw new InvalidOperationException("Unable to reserve subscription usage.");
    }

    private async Task<bool> TryConsumeUsageOnceAsync(
        Guid userId,
        DateTime normalizedPeriod,
        SubscriptionUsageFeature feature,
        int limit,
        CancellationToken ct)
    {

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct);

        var usage = await db.StudentSubscriptionUsages
            .SingleOrDefaultAsync(
                u => u.UserId == userId
                  && u.PeriodStart == normalizedPeriod
                  && u.Feature == feature,
                ct);

        if (usage is null)
        {
            await db.StudentSubscriptionUsages.AddAsync(
                StudentSubscriptionUsage.Create(userId, normalizedPeriod, feature),
                ct);
        }
        else
        {
            if (usage.UsedCount >= limit)
            {
                await transaction.RollbackAsync(ct);
                return false;
            }

            usage.Increment();
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task ReleaseUsageAsync(
        Guid userId,
        DateTime periodStart,
        SubscriptionUsageFeature feature,
        CancellationToken ct = default)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE StudentSubscriptionUsages
            SET UsedCount = CASE WHEN UsedCount > 0 THEN UsedCount - 1 ELSE 0 END,
                UpdatedAt = SYSUTCDATETIME()
            WHERE UserId = {userId}
              AND PeriodStart = {periodStart.Date}
              AND Feature = {feature.ToString()}
              AND IsDeleted = 0
            """, ct);
    }

    public async Task AddSubscriptionAsync(StudentSubscription subscription, CancellationToken ct = default)
        => await db.StudentSubscriptions.AddAsync(subscription, ct);

    public async Task AddPaymentAsync(SubscriptionPayment payment, CancellationToken ct = default)
        => await db.SubscriptionPayments.AddAsync(payment, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
