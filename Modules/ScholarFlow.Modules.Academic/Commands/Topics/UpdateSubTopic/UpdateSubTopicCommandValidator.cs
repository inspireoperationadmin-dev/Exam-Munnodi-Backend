using FluentValidation;

namespace ScholarFlow.Modules.Academic.Commands.Topics.UpdateSubTopic;

public sealed class UpdateSubTopicCommandValidator : AbstractValidator<UpdateSubTopicCommand>
{
    public UpdateSubTopicCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NameEnglish).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameTamil).MaximumLength(200);
        RuleFor(x => x.NameSinhala).MaximumLength(200);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
    }
}
