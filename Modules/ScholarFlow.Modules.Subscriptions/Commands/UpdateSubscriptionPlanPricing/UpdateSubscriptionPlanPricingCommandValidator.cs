using FluentValidation;

namespace ScholarFlow.Modules.Subscriptions.Commands.UpdateSubscriptionPlanPricing;

public sealed class UpdateSubscriptionPlanPricingCommandValidator
    : AbstractValidator<UpdateSubscriptionPlanPricingCommand>
{
    public UpdateSubscriptionPlanPricingCommandValidator()
    {
        RuleFor(x => x.PlanCode).IsInEnum();
        RuleFor(x => x.BasePriceLkr).GreaterThan(0).LessThanOrEqualTo(9_999_999.99m);
        RuleFor(x => x.DiscountPercentage).GreaterThanOrEqualTo(0).LessThan(100);
        RuleFor(x => x.AdminNote).MaximumLength(500);
    }
}
