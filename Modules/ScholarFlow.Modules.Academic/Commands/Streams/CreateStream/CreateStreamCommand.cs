using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Streams.CreateStream;

public sealed record CreateStreamCommand(
    string NameEnglish,
    string? Description = null,
    string? NameTamil = null,
    string? NameSinhala = null) : IRequest<Guid>;
