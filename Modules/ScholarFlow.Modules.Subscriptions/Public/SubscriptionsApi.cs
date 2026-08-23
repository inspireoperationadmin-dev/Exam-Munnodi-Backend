using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Subscriptions.Public;

internal sealed class SubscriptionsApi(
    IApplicationDbContext db,
    ISubscriptionRepository subscriptionRepo) : ISubscriptionsApi
{
    public async Task<SubscriptionAccessSummary> GetAccessSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var subscription = await subscriptionRepo.GetCurrentActiveAsync(userId, ct);
        var upcomingSubscription = await subscriptionRepo.GetNextScheduledAsync(userId, ct);
        var plan = subscription?.Plan ?? await db.SubscriptionPlans
            .AsNoTracking()
            .SingleOrDefaultAsync(
                p => p.Code == SubscriptionPlanCode.Free && p.IsActive,
                ct);

        if (plan is null)
            throw new SubscriptionRequiredException("No active subscription plan is configured.");

        var accessEndsAt = subscription is null
            ? (DateTime?)null
            : await subscriptionRepo.GetActivePlanAccessEndAsync(userId, subscription.PlanId, ct)
              ?? subscription.EndsAt;

        var periodStart = GetSriLankaPeriodStart(now);
        var mockUsed = await subscriptionRepo.GetUsageAsync(
            userId, periodStart, SubscriptionUsageFeature.MockExam, ct);
        var unitUsed = await subscriptionRepo.GetUsageAsync(
            userId, periodStart, SubscriptionUsageFeature.UnitExam, ct);

        int? remaining = subscription is not null
            ? Math.Max(0, (int)Math.Ceiling((accessEndsAt!.Value - now).TotalDays))
            : null;

        return new SubscriptionAccessSummary(
            HasActiveAccess: true,
            Status: subscription is null ? "Free" : "Active",
            PlanCode: plan.Code,
            PlanName: plan.Name,
            Tier: plan.Tier,
            BillingCycle: plan.BillingCycle,
            ProgressAccessLevel: plan.ProgressAccessLevel,
            StartsAt: subscription?.StartsAt,
            EndsAt: accessEndsAt,
            DaysRemaining: remaining,
            ShouldShowSevenDayReminder: remaining is <= 7,
            ShouldShowThreeDayReminder: remaining is <= 3,
            ShouldShowOneDayReminder: remaining is <= 1,
            Entitlements: new SubscriptionEntitlements(
                plan.AllowsPaperExamMode,
                plan.FreePastPaperCount,
                plan.FreeModelPaperCount,
                plan.MonthlyMockExamLimit,
                plan.MonthlyUnitExamLimit),
            Usage: new SubscriptionUsageSummary(mockUsed, unitUsed, periodStart),
            UpcomingSubscription: upcomingSubscription is null
                ? null
                : new UpcomingSubscriptionSummary(
                    upcomingSubscription.Plan.Code,
                    upcomingSubscription.Plan.Name,
                    upcomingSubscription.Plan.Tier,
                    upcomingSubscription.Plan.BillingCycle,
                    upcomingSubscription.StartsAt,
                    upcomingSubscription.EndsAt));
    }

    public async Task EnsureActiveAccessAsync(Guid userId, CancellationToken ct = default)
    {
        var summary = await GetAccessSummaryAsync(userId, ct);

        if (!summary.HasActiveAccess)
            throw new SubscriptionRequiredException("Your access has ended. Choose a plan to continue practising.");
    }

    public async Task<IReadOnlyDictionary<Guid, PaperAccessDecision>> GetPaperAccessAsync(
        Guid userId,
        IReadOnlyCollection<Guid> paperIds,
        CancellationToken ct = default)
    {
        if (paperIds.Count == 0)
            return new Dictionary<Guid, PaperAccessDecision>();

        var summary = await GetAccessSummaryAsync(userId, ct);
        var papers = await db.Papers
            .AsNoTracking()
            .Where(p => paperIds.Contains(p.Id) && p.IsPublic)
            .Select(p => new
            {
                p.Id,
                p.SubjectId,
                p.Type,
                p.Medium,
                p.Year,
                p.CreatedAt
            })
            .ToListAsync(ct);

        if (summary.Tier != SubscriptionTier.Free)
        {
            return papers.ToDictionary(
                p => p.Id,
                p => new PaperAccessDecision(
                    p.Id, false, true, summary.Entitlements.AllowsPaperExamMode, null, null));
        }

        var managedPapers = papers
            .Where(p => p.Type is PaperType.PastPaper or PaperType.ModelPaper
                     && p.SubjectId.HasValue)
            .ToList();

        var subjectIds = managedPapers.Select(p => p.SubjectId!.Value).Distinct().ToList();
        var mediums = managedPapers.Select(p => p.Medium).Distinct().ToList();

        var candidates = subjectIds.Count == 0
            ? []
            : await db.Papers
                .AsNoTracking()
                .Where(p => p.IsPublic
                         && p.SubjectId.HasValue
                         && subjectIds.Contains(p.SubjectId.Value)
                         && mediums.Contains(p.Medium)
                         && (p.Type == PaperType.PastPaper || p.Type == PaperType.ModelPaper))
                .Select(p => new
                {
                    p.Id,
                    SubjectId = p.SubjectId!.Value,
                    p.Type,
                    p.Medium,
                    p.Year,
                    p.CreatedAt
                })
                .ToListAsync(ct);

        var freePaperIds = candidates
            .GroupBy(p => new { p.SubjectId, p.Type, p.Medium })
            .SelectMany(group => group
                .OrderBy(p => p.Year)
                .ThenBy(p => p.CreatedAt)
                .ThenBy(p => p.Id)
                .Take(group.Key.Type == PaperType.PastPaper
                    ? summary.Entitlements.FreePastPaperCount ?? 0
                    : summary.Entitlements.FreeModelPaperCount ?? 0))
            .Select(p => p.Id)
            .ToHashSet();

        return papers.ToDictionary(
            p => p.Id,
            p =>
            {
                if (p.Type is not (PaperType.PastPaper or PaperType.ModelPaper))
                {
                    return new PaperAccessDecision(
                        p.Id, false, true, true, null, null);
                }

                var canPractice = freePaperIds.Contains(p.Id);
                return new PaperAccessDecision(
                    p.Id,
                    IsLocked: !canPractice,
                    CanPractice: canPractice,
                    CanUseExamMode: false,
                    LockReason: canPractice
                        ? "Upgrade to Basic to use paper exam mode."
                        : "The Free plan includes the two oldest papers of each type for this subject.",
                    RequiredPlan: SubscriptionPlanCode.BasicMonthly);
            });
    }

    public async Task EnsureCanStartPaperAsync(
        Guid userId,
        Guid paperId,
        ExamMode mode,
        CancellationToken ct = default)
    {
        var decisions = await GetPaperAccessAsync(userId, [paperId], ct);
        if (!decisions.TryGetValue(paperId, out var access))
            throw new NotFoundException("Paper not found.");

        var allowed = mode switch
        {
            ExamMode.PaperPractice => access.CanPractice,
            ExamMode.PaperExam => access.CanUseExamMode,
            _ => false
        };

        if (!allowed)
            throw new SubscriptionRequiredException(access.LockReason ?? "Upgrade your subscription to access this paper.");
    }

    public async Task<SubscriptionUsageToken?> ConsumeExamUsageAsync(
        Guid userId,
        SubscriptionUsageFeature feature,
        CancellationToken ct = default)
    {
        var summary = await GetAccessSummaryAsync(userId, ct);
        var limit = feature == SubscriptionUsageFeature.MockExam
            ? summary.Entitlements.MonthlyMockExamLimit
            : summary.Entitlements.MonthlyUnitExamLimit;

        if (!limit.HasValue) return null;

        var periodStart = summary.Usage.PeriodStart;
        var consumed = await subscriptionRepo.TryConsumeUsageAsync(
            userId, periodStart, feature, limit.Value, ct);

        if (!consumed)
        {
            var label = feature == SubscriptionUsageFeature.MockExam ? "mock exam" : "unit exam";
            throw new SubscriptionLimitReachedException(
                $"You have reached your monthly {label} limit. Upgrade your plan to continue.");
        }

        return new SubscriptionUsageToken(feature, periodStart);
    }

    public Task ReleaseExamUsageAsync(
        Guid userId,
        SubscriptionUsageToken token,
        CancellationToken ct = default)
        => subscriptionRepo.ReleaseUsageAsync(userId, token.PeriodStart, token.Feature, ct);

    public async Task EnsureProgressAccessAsync(
        Guid userId,
        ProgressAccessLevel requiredLevel,
        CancellationToken ct = default)
    {
        var summary = await GetAccessSummaryAsync(userId, ct);
        if (summary.ProgressAccessLevel < requiredLevel)
            throw new SubscriptionRequiredException("Upgrade your subscription to access these progress details.");
    }

    private static DateTime GetSriLankaPeriodStart(DateTime utcNow)
    {
        var sriLankaNow = utcNow.AddMinutes(330);
        return new DateTime(sriLankaNow.Year, sriLankaNow.Month, 1);
    }
}
