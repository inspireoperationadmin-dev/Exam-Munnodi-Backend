using FluentValidation;

namespace ScholarFlow.Modules.Academic.Commands.Papers.UpdatePaper;

public sealed class UpdatePaperCommandValidator : AbstractValidator<UpdatePaperCommand>
{
    public UpdatePaperCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Year).InclusiveBetween(1950, 2100);
        RuleFor(x => x.NegativeMarkValue).InclusiveBetween(0m, 1m);
        RuleFor(x => x.OfficialPaperCode).MaximumLength(30);
    }
}
