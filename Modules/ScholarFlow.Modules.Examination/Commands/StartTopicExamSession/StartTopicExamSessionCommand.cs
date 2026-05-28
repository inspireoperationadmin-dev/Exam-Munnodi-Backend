using MediatR;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Commands.StartTopicExamSession;

public sealed record StartTopicExamSessionCommand(
    Guid TopicId,
    int Limit,
    bool IsPractice) : IRequest<StartSessionResultDto>;