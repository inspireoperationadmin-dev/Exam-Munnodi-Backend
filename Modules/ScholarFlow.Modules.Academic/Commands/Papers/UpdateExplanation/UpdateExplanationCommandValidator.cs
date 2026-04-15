using FluentValidation;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Modules.Academic.Commands.Papers.UpdateExplanation;

public sealed class UpdateExplanationCommandValidator : AbstractValidator<UpdateExplanationCommand>
{
    public UpdateExplanationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        When(x => x.Type == ExplanationType.Video, () =>
            RuleFor(x => x.VideoUrl).NotEmpty().MaximumLength(1000));
    }
}
