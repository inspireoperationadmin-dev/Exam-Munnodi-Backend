using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Subjects.UpdateSubject;

public sealed record UpdateSubjectCommand(Guid Id, string Name, string? Description) : IRequest;
