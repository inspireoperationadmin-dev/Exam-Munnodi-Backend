using FluentValidation;

namespace ScholarFlow.Modules.Examination.Commands.StartExamSession;

public sealed class StartExamSessionCommandValidator : AbstractValidator<StartExamSessionCommand>
{
    public StartExamSessionCommandValidator()
    {
        RuleFor(x => x.PaperId).NotEmpty();
    }
}
