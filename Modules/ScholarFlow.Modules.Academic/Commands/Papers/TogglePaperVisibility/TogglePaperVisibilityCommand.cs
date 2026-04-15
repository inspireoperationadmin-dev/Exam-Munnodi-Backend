using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Papers.TogglePaperVisibility;

public sealed record TogglePaperVisibilityCommand(Guid Id, bool IsPublic) : IRequest;
