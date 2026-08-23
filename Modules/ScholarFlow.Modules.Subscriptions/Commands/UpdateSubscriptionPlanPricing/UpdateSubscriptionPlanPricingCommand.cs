using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.Subscriptions.DTOs;

namespace ScholarFlow.Modules.Subscriptions.Commands.UpdateSubscriptionPlanPricing;

public sealed record UpdateSubscriptionPlanPricingCommand(
    SubscriptionPlanCode PlanCode,
    decimal BasePriceLkr,
    decimal DiscountPercentage,
    string? AdminNote) : IRequest<SubscriptionPlanDto>;
