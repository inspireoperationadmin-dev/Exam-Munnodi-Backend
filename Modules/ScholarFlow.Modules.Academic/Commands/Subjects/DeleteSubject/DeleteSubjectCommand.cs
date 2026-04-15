using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Subjects.DeleteSubject;

public sealed record DeleteSubjectCommand(Guid Id) : IRequest;
