using FluentValidation;

namespace ScholarFlow.Modules.Examination.Commands.FlagQuestion;

public sealed class FlagQuestionCommandValidator : AbstractValidator<FlagQuestionCommand>
{
    public FlagQuestionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();
    }
}
