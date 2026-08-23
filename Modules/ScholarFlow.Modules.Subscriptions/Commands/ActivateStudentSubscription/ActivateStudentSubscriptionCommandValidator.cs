using FluentValidation;

namespace ScholarFlow.Modules.Subscriptions.Commands.ActivateStudentSubscription;

public sealed class ActivateStudentSubscriptionCommandValidator : AbstractValidator<ActivateStudentSubscriptionCommand>
{
    public ActivateStudentSubscriptionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PlanCode).IsInEnum();
        RuleFor(x => x.BillingPeriods).InclusiveBetween(1, 12);
        RuleFor(x => x.AmountLkr).NotNull().GreaterThan(0);
        RuleFor(x => x.PaymentReference).MaximumLength(120);
        RuleFor(x => x.ReceiptImageUrl).MaximumLength(1000);
        RuleFor(x => x.AdminNote).MaximumLength(1000);
    }
}
