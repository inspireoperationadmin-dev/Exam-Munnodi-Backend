using FluentValidation;

namespace ScholarFlow.Modules.Academic.Commands.Topics.CreateTopic;

public sealed class CreateTopicCommandValidator : AbstractValidator<CreateTopicCommand>
{
    public CreateTopicCommandValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.TopicName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.SubTopics).ChildRules(sub =>
        {
            sub.RuleFor(s => s.Name).NotEmpty().MaximumLength(200);
            sub.RuleFor(s => s.OrderIndex).GreaterThanOrEqualTo(0);
        });
    }
}
