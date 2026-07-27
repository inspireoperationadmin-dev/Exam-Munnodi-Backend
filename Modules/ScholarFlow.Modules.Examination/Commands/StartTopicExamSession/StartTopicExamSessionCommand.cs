using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Commands.StartTopicExamSession;

public sealed record StartTopicExamSessionCommand(
    Guid TopicId,
    int Limit,
    ExamMode Mode) : IRequest<StartSessionResultDto>;
