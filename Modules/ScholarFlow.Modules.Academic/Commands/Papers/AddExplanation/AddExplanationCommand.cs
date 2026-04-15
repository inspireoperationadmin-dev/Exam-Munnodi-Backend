using MediatR;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Modules.Academic.Commands.Papers.AddExplanation;

public sealed record AddExplanationCommand(
    Guid QuestionId,
    ExplanationType Type,
    string? VideoUrl,
    List<AddExplanationSectionItem> Sections) : IRequest<Guid>;

public sealed record AddExplanationSectionItem(string Title, string Content, int OrderIndex);
