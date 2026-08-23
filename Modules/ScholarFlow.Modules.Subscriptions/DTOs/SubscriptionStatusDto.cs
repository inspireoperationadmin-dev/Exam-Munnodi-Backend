using ScholarFlow.Domain.Interfaces;

namespace ScholarFlow.Modules.Subscriptions.DTOs;

public sealed record SubscriptionStatusDto(
    bool HasActiveAccess,
    string Status,
    string? PlanCode,
    string? PlanName,
    string Tier,
    string BillingCycle,
    string ProgressAccessLevel,
    DateTime? StartsAt,
    DateTime? EndsAt,
    int? DaysRemaining,
    bool ShouldShowSevenDayReminder,
    bool ShouldShowThreeDayReminder,
    bool ShouldShowOneDayReminder,
    bool AllowsPaperExamMode,
    int? FreePastPaperCount,
    int? FreeModelPaperCount,
    int? MonthlyMockExamLimit,
    int? MonthlyUnitExamLimit,
    int MockExamsUsed,
    int UnitExamsUsed,
    UpcomingSubscriptionDto? UpcomingSubscription)
{
    public static SubscriptionStatusDto From(SubscriptionAccessSummary summary)
        => new(
            summary.HasActiveAccess,
            summary.Status,
            summary.PlanCode?.ToString(),
            summary.PlanName,
            summary.Tier.ToString(),
            summary.BillingCycle.ToString(),
            summary.ProgressAccessLevel.ToString(),
            summary.StartsAt,
            summary.EndsAt,
            summary.DaysRemaining,
            summary.ShouldShowSevenDayReminder,
            summary.ShouldShowThreeDayReminder,
            summary.ShouldShowOneDayReminder,
            summary.Entitlements.AllowsPaperExamMode,
            summary.Entitlements.FreePastPaperCount,
            summary.Entitlements.FreeModelPaperCount,
            summary.Entitlements.MonthlyMockExamLimit,
            summary.Entitlements.MonthlyUnitExamLimit,
            summary.Usage.MockExamsUsed,
            summary.Usage.UnitExamsUsed,
            summary.UpcomingSubscription is null
                ? null
                : new UpcomingSubscriptionDto(
                    summary.UpcomingSubscription.PlanCode.ToString(),
                    summary.UpcomingSubscription.PlanName,
                    summary.UpcomingSubscription.Tier.ToString(),
                    summary.UpcomingSubscription.BillingCycle.ToString(),
                    summary.UpcomingSubscription.StartsAt,
                    summary.UpcomingSubscription.EndsAt));
}

public sealed record UpcomingSubscriptionDto(
    string PlanCode,
    string PlanName,
    string Tier,
    string BillingCycle,
    DateTime StartsAt,
    DateTime EndsAt);
