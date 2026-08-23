using ScholarFlow.Domain.Entities.Base;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Entities;

public class SubscriptionPlan : AuditableEntity
{
    public SubscriptionPlanCode Code { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public SubscriptionTier Tier { get; private set; }
    public SubscriptionBillingCycle BillingCycle { get; private set; }
    public int DurationDays { get; private set; }
    public decimal BasePriceLkr { get; private set; }
    public decimal DiscountPercentage { get; private set; }
    public decimal PriceLkr { get; private set; }
    public bool AllowsPaperExamMode { get; private set; }
    public int? FreePastPaperCount { get; private set; }
    public int? FreeModelPaperCount { get; private set; }
    public int? MonthlyMockExamLimit { get; private set; }
    public int? MonthlyUnitExamLimit { get; private set; }
    public ProgressAccessLevel ProgressAccessLevel { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }

    private SubscriptionPlan() { }

    public static SubscriptionPlan Create(
        SubscriptionPlanCode code,
        string name,
        SubscriptionTier tier,
        SubscriptionBillingCycle billingCycle,
        int durationDays,
        decimal basePriceLkr,
        decimal discountPercentage,
        bool allowsPaperExamMode,
        int? freePastPaperCount,
        int? freeModelPaperCount,
        int? monthlyMockExamLimit,
        int? monthlyUnitExamLimit,
        ProgressAccessLevel progressAccessLevel,
        int sortOrder)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name.Trim(),
            Tier = tier,
            BillingCycle = billingCycle,
            DurationDays = durationDays,
            BasePriceLkr = basePriceLkr,
            DiscountPercentage = discountPercentage,
            PriceLkr = CalculatePrice(basePriceLkr, discountPercentage),
            AllowsPaperExamMode = allowsPaperExamMode,
            FreePastPaperCount = freePastPaperCount,
            FreeModelPaperCount = freeModelPaperCount,
            MonthlyMockExamLimit = monthlyMockExamLimit,
            MonthlyUnitExamLimit = monthlyUnitExamLimit,
            ProgressAccessLevel = progressAccessLevel,
            IsActive = true,
            SortOrder = sortOrder
        };

    public void Update(
        string name,
        SubscriptionTier tier,
        SubscriptionBillingCycle billingCycle,
        int durationDays,
        bool allowsPaperExamMode,
        int? freePastPaperCount,
        int? freeModelPaperCount,
        int? monthlyMockExamLimit,
        int? monthlyUnitExamLimit,
        ProgressAccessLevel progressAccessLevel,
        bool isActive,
        int sortOrder)
    {
        Name = name.Trim();
        Tier = tier;
        BillingCycle = billingCycle;
        DurationDays = durationDays;
        AllowsPaperExamMode = allowsPaperExamMode;
        FreePastPaperCount = freePastPaperCount;
        FreeModelPaperCount = freeModelPaperCount;
        MonthlyMockExamLimit = monthlyMockExamLimit;
        MonthlyUnitExamLimit = monthlyUnitExamLimit;
        ProgressAccessLevel = progressAccessLevel;
        IsActive = isActive;
        SortOrder = sortOrder;
    }

    public void UpdatePricing(decimal basePriceLkr, decimal discountPercentage)
    {
        if (Tier == SubscriptionTier.Free)
            throw new InvalidOperationException("Free plan pricing cannot be changed.");
        if (basePriceLkr <= 0)
            throw new ArgumentOutOfRangeException(nameof(basePriceLkr));
        if (discountPercentage is < 0 or >= 100)
            throw new ArgumentOutOfRangeException(nameof(discountPercentage));

        BasePriceLkr = basePriceLkr;
        DiscountPercentage = discountPercentage;
        PriceLkr = CalculatePrice(basePriceLkr, discountPercentage);
    }

    private static decimal CalculatePrice(decimal basePriceLkr, decimal discountPercentage)
        => decimal.Round(
            basePriceLkr * (100m - discountPercentage) / 100m,
            2,
            MidpointRounding.AwayFromZero);

    public void Deactivate() => IsActive = false;
}
