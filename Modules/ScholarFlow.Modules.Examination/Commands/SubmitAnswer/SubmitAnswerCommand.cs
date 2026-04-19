using MediatR;

namespace ScholarFlow.Modules.Examination.Commands.SubmitAnswer;

public sealed record SubmitAnswerCommand(
    Guid SessionId,
    Guid QuestionId,
    Guid? SelectedOptionId,   // null = clear answer
    int TimeSpentSeconds) : IRequest;
