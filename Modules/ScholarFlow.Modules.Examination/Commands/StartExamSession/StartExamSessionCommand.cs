using MediatR;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Commands.StartExamSession;

public sealed record StartExamSessionCommand(
    Guid PaperId,
    bool IsPractice) : IRequest<StartSessionResultDto>;
