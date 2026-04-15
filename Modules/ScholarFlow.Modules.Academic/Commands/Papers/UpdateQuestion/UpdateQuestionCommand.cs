using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Papers.UpdateQuestion;

public sealed record UpdateQuestionCommand(
    Guid Id,
    Guid SubTopicId,
    string QuestionText,
    string? QuestionImageUrl,
    int OrderIndex) : IRequest;
