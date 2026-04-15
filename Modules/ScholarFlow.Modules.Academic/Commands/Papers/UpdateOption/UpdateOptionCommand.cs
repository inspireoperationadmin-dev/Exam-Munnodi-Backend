using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Papers.UpdateOption;

public sealed record UpdateOptionCommand(
    Guid Id,
    string OptionText,
    string? OptionImageUrl,
    bool IsCorrect) : IRequest;
