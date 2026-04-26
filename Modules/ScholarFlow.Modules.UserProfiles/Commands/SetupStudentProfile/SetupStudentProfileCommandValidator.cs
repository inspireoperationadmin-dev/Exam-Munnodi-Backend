using FluentValidation;

namespace ScholarFlow.Modules.UserProfiles.Commands.SetupStudentProfile;

public sealed class SetupStudentProfileCommandValidator
    : AbstractValidator<SetupStudentProfileCommand>
{
    public SetupStudentProfileCommandValidator()
    {
        RuleFor(x => x.StreamId).NotEmpty();
        RuleFor(x => x.ExamYear).InclusiveBetween(2020, 2035);
        RuleFor(x => x.SubjectIds)
            .Must(ids => ids.Count == 3)
            .WithMessage("Exactly 3 subjects must be selected.");
        RuleForEach(x => x.SubjectIds).NotEmpty();
    }
}