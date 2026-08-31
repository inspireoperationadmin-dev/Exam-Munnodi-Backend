using FluentValidation;

namespace ScholarFlow.Modules.Identity.Commands.VerifyOtp;

public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress().WithMessage("A valid email is required.")
            .MaximumLength(254);

        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches("^[0-9]{6}$").WithMessage("The verification code must contain 6 digits.");
    }
}
