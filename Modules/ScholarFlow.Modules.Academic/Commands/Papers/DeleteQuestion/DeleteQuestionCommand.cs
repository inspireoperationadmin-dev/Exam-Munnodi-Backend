using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Papers.DeleteQuestion;

public sealed record DeleteQuestionCommand(Guid Id) : IRequest;
