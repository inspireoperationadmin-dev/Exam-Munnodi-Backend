using MediatR;

namespace ScholarFlow.Modules.Examination.Commands.FlagQuestion;

public sealed record FlagQuestionCommand(
    Guid SessionId,
    Guid QuestionId,
    bool Flagged) : IRequest;
