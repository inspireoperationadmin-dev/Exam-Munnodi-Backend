using FluentValidation;

namespace ScholarFlow.Modules.Academic.Commands.Subjects.CreateSubject;

public sealed class CreateSubjectCommandValidator : AbstractValidator<CreateSubjectCommand>
{
    public CreateSubjectCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.StreamIds).NotNull();
        RuleForEach(x => x.StreamIds).NotEmpty();
    }
}
