using FluentValidation;

namespace ScholarFlow.Modules.Subscriptions.Commands.CancelStudentSubscription;

public sealed class CancelStudentSubscriptionCommandValidator : AbstractValidator<CancelStudentSubscriptionCommand>
{
    public CancelStudentSubscriptionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.SubscriptionId).NotEmpty();
        RuleFor(x => x.AdminNote).MaximumLength(1000);
    }
}
