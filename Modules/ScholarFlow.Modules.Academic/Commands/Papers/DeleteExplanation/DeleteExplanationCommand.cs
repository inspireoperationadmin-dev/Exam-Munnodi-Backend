using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Papers.DeleteExplanation;

public sealed record DeleteExplanationCommand(Guid Id) : IRequest;
