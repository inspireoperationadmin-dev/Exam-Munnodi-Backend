using FluentValidation;

namespace ScholarFlow.Modules.Academic.Commands.Topics.CreateSubTopic;

public sealed class CreateSubTopicCommandValidator : AbstractValidator<CreateSubTopicCommand>
{
    public CreateSubTopicCommandValidator()
    {
        RuleFor(x => x.TopicId).NotEmpty();
        RuleFor(x => x.NameEnglish).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameTamil).MaximumLength(200);
        RuleFor(x => x.NameSinhala).MaximumLength(200);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
    }
}
