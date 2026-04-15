using FluentValidation;

namespace ScholarFlow.Modules.Academic.Commands.Papers.UpdateQuestion;

public sealed class UpdateQuestionCommandValidator : AbstractValidator<UpdateQuestionCommand>
{
    public UpdateQuestionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SubTopicId).NotEmpty();
        RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.QuestionImageUrl).MaximumLength(1000);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(1);
    }
}
