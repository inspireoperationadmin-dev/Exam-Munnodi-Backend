using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Streams.UpdateStream;

public sealed record UpdateStreamCommand(
    Guid Id,
    string NameEnglish,
    string? Description = null,
    string? NameTamil = null,
    string? NameSinhala = null) : IRequest;
