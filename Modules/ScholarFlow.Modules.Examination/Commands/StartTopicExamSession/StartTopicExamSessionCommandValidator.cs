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
            .Must(mode => mode is ExamMode.Practice or ExamMode.FixedExam)
            .WithMessage("Topic sessions support only Practice or FixedExam mode.");
    }
}
