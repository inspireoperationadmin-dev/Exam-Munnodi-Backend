using MediatR;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Modules.Academic.Commands.Papers.UpdateExplanation;

public sealed record UpdateExplanationCommand(Guid Id, ExplanationType Type, string? VideoUrl) : IRequest;
