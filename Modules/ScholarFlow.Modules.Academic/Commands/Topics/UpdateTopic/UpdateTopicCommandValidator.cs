using FluentValidation;

namespace ScholarFlow.Modules.Academic.Commands.Topics.UpdateTopic;

public sealed class UpdateTopicCommandValidator : AbstractValidator<UpdateTopicCommand>
{
    public UpdateTopicCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TopicName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
    }
}
