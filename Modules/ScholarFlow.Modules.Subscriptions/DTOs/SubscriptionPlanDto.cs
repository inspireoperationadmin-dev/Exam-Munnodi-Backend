namespace ScholarFlow.Modules.Subscriptions.DTOs;

public sealed record SubscriptionPlanDto(
    Guid Id,
    string Code,
    string Name,
    string Tier,
    string BillingCycle,
    int DurationDays,
    decimal BasePriceLkr,
    decimal DiscountPercentage,
    decimal PriceLkr,
    bool AllowsPaperExamMode,
    int? FreePastPaperCount,
    int? FreeModelPaperCount,
    int? MonthlyMockExamLimit,
    int? MonthlyUnitExamLimit,
    string ProgressAccessLevel,
    bool IsFree,
    bool IsActive);
