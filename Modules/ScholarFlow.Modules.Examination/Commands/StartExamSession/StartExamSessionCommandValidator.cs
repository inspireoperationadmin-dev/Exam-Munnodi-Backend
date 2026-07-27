using FluentValidation;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Modules.Examination.Commands.StartExamSession;

public sealed class StartExamSessionCommandValidator : AbstractValidator<StartExamSessionCommand>
{
    public StartExamSessionCommandValidator()
    {
        RuleFor(x => x.PaperId).NotEmpty();
        RuleFor(x => x.Mode)
            .Must(mode => mode is ExamMode.Practice or ExamMode.FixedExam)
            .WithMessage("Paper sessions support only Practice or FixedExam mode.");
    }
}
