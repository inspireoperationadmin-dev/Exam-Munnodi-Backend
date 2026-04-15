using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Streams.CreateStream;

public sealed record CreateStreamCommand(string Name, string? Description) : IRequest<Guid>;
