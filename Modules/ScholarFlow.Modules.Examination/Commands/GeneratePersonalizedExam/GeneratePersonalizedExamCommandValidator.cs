using FluentValidation;

namespace ScholarFlow.Modules.Examination.Commands.GeneratePersonalizedExam;

public sealed class GeneratePersonalizedExamCommandValidator : AbstractValidator<GeneratePersonalizedExamCommand>
{
    public GeneratePersonalizedExamCommandValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.QuestionCount)
            .Must(count => count is 10 or 20 or 30 or 50)
            .WithMessage("Question count must be 10, 20, 30, or 50.");
    }
}
