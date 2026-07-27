using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Subjects.CreateSubject;

public sealed record CreateSubjectCommand(
    List<Guid> StreamIds,
    string NameEnglish,
    string? Description = null,
    string? NameTamil = null,
    string? NameSinhala = null) : IRequest<Guid>;
