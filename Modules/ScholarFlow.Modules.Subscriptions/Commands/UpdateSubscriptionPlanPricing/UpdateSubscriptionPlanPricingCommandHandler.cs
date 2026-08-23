using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Subscriptions.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Subscriptions.Commands.UpdateSubscriptionPlanPricing;

public sealed class UpdateSubscriptionPlanPricingCommandHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateSubscriptionPlanPricingCommand, SubscriptionPlanDto>
{
    public async Task<SubscriptionPlanDto> Handle(
        UpdateSubscriptionPlanPricingCommand request,
        CancellationToken ct)
    {
        var plan = await db.SubscriptionPlans
            .SingleOrDefaultAsync(p => p.Code == request.PlanCode, ct)
            ?? throw new NotFoundException("Subscription plan not found.");

        if (plan.Tier == SubscriptionTier.Free)
            throw new BadRequestException("The Free plan price cannot be changed.");

        if (plan.BasePriceLkr != request.BasePriceLkr
            || plan.DiscountPercentage != request.DiscountPercentage)
        {
            var previousBasePrice = plan.BasePriceLkr;
            var previousDiscount = plan.DiscountPercentage;
            var previousPrice = plan.PriceLkr;

            plan.UpdatePricing(request.BasePriceLkr, request.DiscountPercentage);

            await db.SubscriptionPlanPriceChanges.AddAsync(
                SubscriptionPlanPriceChange.Create(
                    plan.Id,
                    previousBasePrice,
                    previousDiscount,
                    previousPrice,
                    plan.BasePriceLkr,
                    plan.DiscountPercentage,
                    plan.PriceLkr,
                    currentUser.UserId,
                    request.AdminNote),
                ct);

            await db.SaveChangesAsync(ct);
        }

        return new SubscriptionPlanDto(
            plan.Id,
            plan.Code.ToString(),
            plan.Name,
            plan.Tier.ToString(),
            plan.BillingCycle.ToString(),
            plan.DurationDays,
            plan.BasePriceLkr,
            plan.DiscountPercentage,
            plan.PriceLkr,
            plan.AllowsPaperExamMode,
            plan.FreePastPaperCount,
            plan.FreeModelPaperCount,
            plan.MonthlyMockExamLimit,
            plan.MonthlyUnitExamLimit,
            plan.ProgressAccessLevel.ToString(),
            plan.Tier == SubscriptionTier.Free,
            plan.IsActive);
    }
}
