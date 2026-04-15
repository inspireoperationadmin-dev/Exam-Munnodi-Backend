using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Streams.DeleteStream;

public sealed record DeleteStreamCommand(Guid Id) : IRequest;
