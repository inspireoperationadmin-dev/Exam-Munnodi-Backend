using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Subjects.RemoveSubjectFromStream;

public sealed record RemoveSubjectFromStreamCommand(Guid SubjectId, Guid StreamId) : IRequest;
