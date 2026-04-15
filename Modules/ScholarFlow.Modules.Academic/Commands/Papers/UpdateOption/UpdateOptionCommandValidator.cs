using FluentValidation;

namespace ScholarFlow.Modules.Academic.Commands.Papers.UpdateOption;

public sealed class UpdateOptionCommandValidator : AbstractValidator<UpdateOptionCommand>
{
    public UpdateOptionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.OptionText).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.OptionImageUrl).MaximumLength(1000);
    }
}
