using FluentValidation;

namespace ScholarFlow.Modules.Academic.Commands.Streams.UpdateStream;

public sealed class UpdateStreamCommandValidator : AbstractValidator<UpdateStreamCommand>
{
    public UpdateStreamCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
