using FluentValidation;

namespace ScholarFlow.Modules.Examination.Commands.EndExamSession;

public sealed class EndExamSessionCommandValidator : AbstractValidator<EndExamSessionCommand>
{
    public EndExamSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}
