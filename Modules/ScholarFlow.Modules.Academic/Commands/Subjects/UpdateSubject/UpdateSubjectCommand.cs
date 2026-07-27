using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Subjects.UpdateSubject;

public sealed record UpdateSubjectCommand(
    Guid Id,
    string NameEnglish,
    string? Description = null,
    string? NameTamil = null,
    string? NameSinhala = null) : IRequest;
