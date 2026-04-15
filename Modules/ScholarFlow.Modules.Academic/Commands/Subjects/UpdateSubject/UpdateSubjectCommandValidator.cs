using FluentValidation;

namespace ScholarFlow.Modules.Academic.Commands.Subjects.UpdateSubject;

public sealed class UpdateSubjectCommandValidator : AbstractValidator<UpdateSubjectCommand>
{
    public UpdateSubjectCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
