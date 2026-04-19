using FluentValidation;

namespace ScholarFlow.Modules.Examination.Commands.SubmitAnswer;

public sealed class SubmitAnswerCommandValidator : AbstractValidator<SubmitAnswerCommand>
{
    public SubmitAnswerCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.TimeSpentSeconds).GreaterThanOrEqualTo(0);
    }
}
