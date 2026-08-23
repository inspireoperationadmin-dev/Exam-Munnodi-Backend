using ScholarFlow.Domain.Entities.Base;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Entities;

public class StudentSubscription : AuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public Guid? ActivatedByAdminId { get; private set; }
    public DateTime ActivatedAt { get; private set; }
    public SubscriptionActivationSource ActivationSource { get; private set; }
    public string? PromotionCode { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public Guid? CancelledByAdminId { get; private set; }

    public ApplicationUser User { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;

    private StudentSubscription() { }

    public static StudentSubscription Activate(
        Guid userId,
        Guid planId,
        DateTime startsAt,
        DateTime endsAt,
        Guid? activatedByAdminId,
        string? notes,
        SubscriptionActivationSource activationSource = SubscriptionActivationSource.AdminManual,
        string? promotionCode = null)
    {
        var now = DateTime.UtcNow;

        return new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = planId,
            Status = SubscriptionStatus.Active,
            StartsAt = startsAt,
            EndsAt = endsAt,
            ActivatedByAdminId = activatedByAdminId,
            ActivatedAt = now,
            ActivationSource = activationSource,
            PromotionCode = string.IsNullOrWhiteSpace(promotionCode) ? null : promotionCode.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }

    public void Cancel(Guid cancelledByAdminId, string? notes)
    {
        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancelledByAdminId = cancelledByAdminId;

        if (!string.IsNullOrWhiteSpace(notes))
            Notes = string.IsNullOrWhiteSpace(Notes) ? notes.Trim() : $"{Notes}\n{notes.Trim()}";
    }
}
