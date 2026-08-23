using FluentValidation;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Modules.Examination.Commands.StartExamSession;

public sealed class StartExamSessionCommandValidator : AbstractValidator<StartExamSessionCommand>
{
    public StartExamSessionCommandValidator()
    {
        RuleFor(x => x.PaperId).NotEmpty();
        RuleFor(x => x.Mode)
            .Must(mode => mode.IsPaperMode())
            .WithMessage("Paper sessions support only PaperPractice or PaperExam mode.");
        RuleFor(x => x.ReplaceSessionId)
            .NotEqual(Guid.Empty)
            .When(x => x.ReplaceSessionId.HasValue);
        RuleFor(x => x.ReplaceSessionId)
            .Null()
            .When(x => x.Mode == ExamMode.PaperExam)
            .WithMessage("A timed paper exam cannot replace another session.");
    }
}
