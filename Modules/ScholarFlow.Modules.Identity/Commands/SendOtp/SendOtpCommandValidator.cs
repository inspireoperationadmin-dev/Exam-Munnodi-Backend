using FluentValidation;

namespace ScholarFlow.Modules.Identity.Commands.SendOtp;

public sealed class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress().WithMessage("A valid email is required.")
            .MaximumLength(254);
    }
}
