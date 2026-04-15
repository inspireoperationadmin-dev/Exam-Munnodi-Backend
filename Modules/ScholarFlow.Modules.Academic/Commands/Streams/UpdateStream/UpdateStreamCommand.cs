using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Streams.UpdateStream;

public sealed record UpdateStreamCommand(Guid Id, string Name, string? Description) : IRequest;
