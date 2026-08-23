namespace ScholarFlow.Modules.Subscriptions.DTOs;

public sealed record AdminSubscriptionPeriodDto(
    Guid SubscriptionId,
    string PlanCode,
    string PlanName,
    string Tier,
    string BillingCycle,
    string PeriodState,
    DateTime StartsAt,
    DateTime EndsAt,
    string ActivationSource,
    decimal? PaymentAmountLkr,
    string? PaymentReference,
    DateTime? PaymentReviewedAt);
