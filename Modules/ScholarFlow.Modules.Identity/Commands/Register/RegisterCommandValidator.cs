using FluentValidation;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Modules.Identity.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.RegistrationTicket)
            .NotEmpty().WithMessage("A verified registration ticket is required.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress().WithMessage("A valid email is required.")
            .MaximumLength(254);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100);

        When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
        {
            RuleFor(x => x.PhoneNumber!)
                .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.")
                .Matches("^[0-9+()\\s-]{7,20}$")
                .WithMessage("Enter a valid phone number using digits and an optional country code.");
        });

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => r == AppRole.Student || r == AppRole.Teacher)
            .WithMessage($"Role must be '{AppRole.Student}' or '{AppRole.Teacher}'.");

        // Teacher-specific rules
        When(x => x.Role == AppRole.Teacher, () =>
        {
            RuleFor(x => x.SubjectId)
                .NotEmpty().WithMessage("SubjectId is required for teacher registration.");

            RuleFor(x => x.Qualification)
                .NotEmpty().WithMessage("Qualification is required for teacher registration.")
                .MaximumLength(200);

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required for teacher registration.");
        });
    }
}
