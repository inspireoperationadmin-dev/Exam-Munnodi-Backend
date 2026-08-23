using ScholarFlow.Domain.Entities.Base;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Entities;

public class SubscriptionPayment : AuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid? SubscriptionId { get; private set; }
    public decimal AmountLkr { get; private set; }
    public SubscriptionPaymentMethod PaymentMethod { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? ReceiptImageUrl { get; private set; }
    public SubscriptionPaymentStatus Status { get; private set; }
    public string? AdminNote { get; private set; }
    public Guid ReviewedByAdminId { get; private set; }
    public DateTime ReviewedAt { get; private set; }

    public ApplicationUser User { get; set; } = null!;
    public StudentSubscription? Subscription { get; set; }

    private SubscriptionPayment() { }

    public static SubscriptionPayment ApprovedBankTransfer(
        Guid userId,
        Guid subscriptionId,
        decimal amountLkr,
        string? referenceNumber,
        string? receiptImageUrl,
        string? adminNote,
        Guid reviewedByAdminId)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubscriptionId = subscriptionId,
            AmountLkr = amountLkr,
            PaymentMethod = SubscriptionPaymentMethod.BankTransfer,
            ReferenceNumber = string.IsNullOrWhiteSpace(referenceNumber) ? null : referenceNumber.Trim(),
            ReceiptImageUrl = string.IsNullOrWhiteSpace(receiptImageUrl) ? null : receiptImageUrl.Trim(),
            Status = SubscriptionPaymentStatus.Approved,
            AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim(),
            ReviewedByAdminId = reviewedByAdminId,
            ReviewedAt = DateTime.UtcNow
        };
}
