using MediatR;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Commands.EndExamSession;

public sealed record SubmittedAnswerDto(
    Guid QuestionId,
    Guid? SelectedOptionId,
    int TimeSpentSeconds);

public sealed record EndExamSessionCommand(
    Guid SessionId,
    List<SubmittedAnswerDto> SubmittedAnswers) : IRequest<EndSessionResultDto>;