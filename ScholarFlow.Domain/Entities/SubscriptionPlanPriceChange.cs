using ScholarFlow.Domain.Entities.Base;

namespace ScholarFlow.Domain.Entities;

public sealed class SubscriptionPlanPriceChange : AuditableEntity
{
    public Guid PlanId { get; private set; }
    public decimal PreviousBasePriceLkr { get; private set; }
    public decimal PreviousDiscountPercentage { get; private set; }
    public decimal PreviousPriceLkr { get; private set; }
    public decimal NewBasePriceLkr { get; private set; }
    public decimal NewDiscountPercentage { get; private set; }
    public decimal NewPriceLkr { get; private set; }
    public Guid ChangedByAdminId { get; private set; }
    public string? Note { get; private set; }

    public SubscriptionPlan Plan { get; private set; } = null!;

    private SubscriptionPlanPriceChange() { }

    public static SubscriptionPlanPriceChange Create(
        Guid planId,
        decimal previousBasePriceLkr,
        decimal previousDiscountPercentage,
        decimal previousPriceLkr,
        decimal newBasePriceLkr,
        decimal newDiscountPercentage,
        decimal newPriceLkr,
        Guid changedByAdminId,
        string? note)
        => new()
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            PreviousBasePriceLkr = previousBasePriceLkr,
            PreviousDiscountPercentage = previousDiscountPercentage,
            PreviousPriceLkr = previousPriceLkr,
            NewBasePriceLkr = newBasePriceLkr,
            NewDiscountPercentage = newDiscountPercentage,
            NewPriceLkr = newPriceLkr,
            ChangedByAdminId = changedByAdminId,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };
}
