using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Subjects.AssignSubjectToStream;

public sealed record AssignSubjectToStreamCommand(Guid SubjectId, Guid StreamId) : IRequest;
