using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Interfaces;

public interface ISubscriptionsApi
{
    Task<SubscriptionAccessSummary> GetAccessSummaryAsync(Guid userId, CancellationToken ct = default);
    Task EnsureActiveAccessAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, PaperAccessDecision>> GetPaperAccessAsync(
        Guid userId,
        IReadOnlyCollection<Guid> paperIds,
        CancellationToken ct = default);
    Task EnsureCanStartPaperAsync(Guid userId, Guid paperId, ExamMode mode, CancellationToken ct = default);
    Task<SubscriptionUsageToken?> ConsumeExamUsageAsync(
        Guid userId,
        SubscriptionUsageFeature feature,
        CancellationToken ct = default);
    Task ReleaseExamUsageAsync(Guid userId, SubscriptionUsageToken token, CancellationToken ct = default);
    Task EnsureProgressAccessAsync(Guid userId, ProgressAccessLevel requiredLevel, CancellationToken ct = default);
}

public sealed record SubscriptionAccessSummary(
    bool HasActiveAccess,
    string Status,
    SubscriptionPlanCode? PlanCode,
    string? PlanName,
    SubscriptionTier Tier,
    SubscriptionBillingCycle BillingCycle,
    ProgressAccessLevel ProgressAccessLevel,
    DateTime? StartsAt,
    DateTime? EndsAt,
    int? DaysRemaining,
    bool ShouldShowSevenDayReminder,
    bool ShouldShowThreeDayReminder,
    bool ShouldShowOneDayReminder,
    SubscriptionEntitlements Entitlements,
    SubscriptionUsageSummary Usage,
    UpcomingSubscriptionSummary? UpcomingSubscription);

public sealed record UpcomingSubscriptionSummary(
    SubscriptionPlanCode PlanCode,
    string PlanName,
    SubscriptionTier Tier,
    SubscriptionBillingCycle BillingCycle,
    DateTime StartsAt,
    DateTime EndsAt);

public sealed record SubscriptionEntitlements(
    bool AllowsPaperExamMode,
    int? FreePastPaperCount,
    int? FreeModelPaperCount,
    int? MonthlyMockExamLimit,
    int? MonthlyUnitExamLimit);

public sealed record SubscriptionUsageSummary(
    int MockExamsUsed,
    int UnitExamsUsed,
    DateTime PeriodStart);

public sealed record PaperAccessDecision(
    Guid PaperId,
    bool IsLocked,
    bool CanPractice,
    bool CanUseExamMode,
    string? LockReason,
    SubscriptionPlanCode? RequiredPlan);

public sealed record SubscriptionUsageToken(
    SubscriptionUsageFeature Feature,
    DateTime PeriodStart);
