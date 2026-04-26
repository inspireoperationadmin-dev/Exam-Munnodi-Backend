using MediatR;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Modules.UserProfiles.Commands.SetupStudentProfile;

public sealed record SetupStudentProfileCommand(
    Guid              StreamId,
    PaperMedium       Medium,
    int               ExamYear,
    List<Guid>        SubjectIds) : IRequest;