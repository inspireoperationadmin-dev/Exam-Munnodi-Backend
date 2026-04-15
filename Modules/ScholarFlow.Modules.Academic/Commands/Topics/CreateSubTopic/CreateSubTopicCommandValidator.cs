using FluentValidation;

namespace ScholarFlow.Modules.Academic.Commands.Topics.CreateSubTopic;

public sealed class CreateSubTopicCommandValidator : AbstractValidator<CreateSubTopicCommand>
{
    public CreateSubTopicCommandValidator()
    {
        RuleFor(x => x.TopicId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
    }
}
