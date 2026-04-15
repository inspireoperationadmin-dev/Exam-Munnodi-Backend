using FluentValidation;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Modules.Academic.Commands.Papers.AddExplanation;

public sealed class AddExplanationCommandValidator : AbstractValidator<AddExplanationCommand>
{
    public AddExplanationCommandValidator()
    {
        RuleFor(x => x.QuestionId).NotEmpty();
        When(x => x.Type == ExplanationType.Video, () =>
            RuleFor(x => x.VideoUrl).NotEmpty().MaximumLength(1000));
        When(x => x.Type == ExplanationType.Text, () =>
            RuleFor(x => x.Sections).NotEmpty().WithMessage("Text explanation must have at least one section."));
        RuleForEach(x => x.Sections).ChildRules(sec =>
        {
            sec.RuleFor(s => s.Title).NotEmpty().MaximumLength(200);
            sec.RuleFor(s => s.Content).NotEmpty();
            sec.RuleFor(s => s.OrderIndex).GreaterThanOrEqualTo(1);
        });
    }
}
