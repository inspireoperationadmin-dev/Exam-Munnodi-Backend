using FluentValidation;

namespace ScholarFlow.Modules.Academic.Commands.Topics.CreateTopic;

public sealed class CreateTopicCommandValidator : AbstractValidator<CreateTopicCommand>
{
    public CreateTopicCommandValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.NameEnglish).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameTamil).MaximumLength(200);
        RuleFor(x => x.NameSinhala).MaximumLength(200);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.SubTopics).ChildRules(sub =>
        {
            sub.RuleFor(s => s.NameEnglish).NotEmpty().MaximumLength(200);
            sub.RuleFor(s => s.NameTamil).MaximumLength(200);
            sub.RuleFor(s => s.NameSinhala).MaximumLength(200);
            sub.RuleFor(s => s.OrderIndex).GreaterThanOrEqualTo(0);
        });
    }
}
