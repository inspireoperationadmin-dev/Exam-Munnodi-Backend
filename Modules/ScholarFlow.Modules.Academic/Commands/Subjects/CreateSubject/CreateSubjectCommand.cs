using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Subjects.CreateSubject;

public sealed record CreateSubjectCommand(
    string Name,
    string? Description,
    List<Guid> StreamIds) : IRequest<Guid>;
