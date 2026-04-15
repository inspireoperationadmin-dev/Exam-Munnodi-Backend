using FluentValidation;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Modules.Identity.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress().WithMessage("A valid email is required.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100);

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
