using FluentValidation;

namespace ScholarFlow.Modules.Academic.Commands.Streams.CreateStream;

public sealed class CreateStreamCommandValidator : AbstractValidator<CreateStreamCommand>
{
    public CreateStreamCommandValidator()
    {
        RuleFor(x => x.NameEnglish).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NameTamil).MaximumLength(100);
        RuleFor(x => x.NameSinhala).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
