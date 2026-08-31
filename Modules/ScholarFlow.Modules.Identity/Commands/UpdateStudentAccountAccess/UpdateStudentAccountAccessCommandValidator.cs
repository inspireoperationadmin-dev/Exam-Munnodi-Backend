using FluentValidation;

namespace ScholarFlow.Modules.Identity.Commands.UpdateStudentAccountAccess;

public sealed class UpdateStudentAccountAccessCommandValidator
    : AbstractValidator<UpdateStudentAccountAccessCommand>
{
    public UpdateStudentAccountAccessCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Action).IsInEnum();

        When(x => x.Action == StudentAccountAccessAction.Suspend, () =>
        {
            RuleFor(x => x.SuspendedUntil)
                .NotNull().WithMessage("A suspension end date is required.")
                .Must(until => until > DateTimeOffset.UtcNow)
                .WithMessage("The suspension end date must be in the future.");
        });

        When(x => x.Action != StudentAccountAccessAction.Suspend, () =>
        {
            RuleFor(x => x.SuspendedUntil)
                .Null().WithMessage("A suspension end date is only valid when suspending an account.");
        });
    }
}
