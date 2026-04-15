using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Papers.AddQuestionToPaper;

public sealed record AddQuestionToPaperCommand(
    Guid PaperId,
    Guid SubTopicId,
    string QuestionText,
    string? QuestionImageUrl,
    int OrderIndex,
    List<AddOptionItem> Options) : IRequest<Guid>;

public sealed record AddOptionItem(
    string Label,
    string OptionText,
    string? OptionImageUrl,
    bool IsCorrect);
