using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.Subscriptions.DTOs;

namespace ScholarFlow.Modules.Subscriptions.Commands.ActivateStudentSubscription;

public sealed record ActivateStudentSubscriptionCommand(
    Guid UserId,
    SubscriptionPlanCode PlanCode,
    int BillingPeriods,
    decimal? AmountLkr,
    string? PaymentReference,
    string? ReceiptImageUrl,
    string? AdminNote,
    DateTime? StartsAt) : IRequest<SubscriptionStatusDto>;
