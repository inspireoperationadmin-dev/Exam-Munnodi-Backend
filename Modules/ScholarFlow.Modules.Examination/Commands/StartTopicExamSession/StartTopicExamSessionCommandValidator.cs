using FluentValidation;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Modules.Examination.Commands.StartTopicExamSession;

public sealed class StartTopicExamSessionCommandValidator : AbstractValidator<StartTopicExamSessionCommand>
{
    public StartTopicExamSessionCommandValidator()
    {
        RuleFor(x => x.TopicId).NotEmpty();
        RuleFor(x => x.Limit).InclusiveBetween(1, 50);
        RuleFor(x => x.Mode)
            .Must(mode => mode.IsTopicMode())
            .WithMessage("Topic sessions support only TopicExam or TopicPractice mode.");
        RuleFor(x => x.ReplaceSessionId)
            .NotEqual(Guid.Empty)
            .When(x => x.ReplaceSessionId.HasValue);
        RuleFor(x => x.ReplaceSessionId)
            .Null()
            .When(x => x.Mode == ExamMode.TopicExam)
            .WithMessage("A timed unit exam cannot replace another session.");
    }
}
