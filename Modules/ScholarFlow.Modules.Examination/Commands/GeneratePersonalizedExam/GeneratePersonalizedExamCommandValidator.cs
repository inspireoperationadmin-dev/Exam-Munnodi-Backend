using FluentValidation;

namespace ScholarFlow.Modules.Examination.Commands.GeneratePersonalizedExam;

public sealed class GeneratePersonalizedExamCommandValidator : AbstractValidator<GeneratePersonalizedExamCommand>
{
    public GeneratePersonalizedExamCommandValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty();
    }
}
