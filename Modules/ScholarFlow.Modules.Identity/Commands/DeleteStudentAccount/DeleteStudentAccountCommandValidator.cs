using FluentValidation;

namespace ScholarFlow.Modules.Identity.Commands.DeleteStudentAccount;

public sealed class DeleteStudentAccountCommandValidator
    : AbstractValidator<DeleteStudentAccountCommand>
{
    public DeleteStudentAccountCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.ConfirmationEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
    }
}
