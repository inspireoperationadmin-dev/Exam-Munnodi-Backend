using FluentValidation;

namespace ScholarFlow.Modules.Academic.Commands.Papers.AddQuestionToPaper;

public sealed class AddQuestionToPaperCommandValidator : AbstractValidator<AddQuestionToPaperCommand>
{
    public AddQuestionToPaperCommandValidator()
    {
        RuleFor(x => x.PaperId).NotEmpty();
        RuleFor(x => x.SubTopicId).NotEmpty();
        RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.QuestionImageUrl).MaximumLength(1000);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Options)
            .Must(opts => opts.Count == 5).WithMessage("A question must have exactly 5 options.")
            .Must(opts => opts.Count(o => o.IsCorrect) == 1).WithMessage("Exactly one option must be correct.");
        RuleForEach(x => x.Options).ChildRules(opt =>
        {
            opt.RuleFor(o => o.Label).NotEmpty().MaximumLength(5);
            opt.RuleFor(o => o.OptionText).MaximumLength(1000);
            opt.RuleFor(o => o).Must(o => !string.IsNullOrWhiteSpace(o.OptionText) || !string.IsNullOrWhiteSpace(o.OptionImageUrl))
               .WithMessage("Each option must have either text or an image URL.");
        });
    }
}
