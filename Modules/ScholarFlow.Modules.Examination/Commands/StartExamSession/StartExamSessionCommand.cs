using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Commands.StartExamSession;

public sealed record StartExamSessionCommand(
    Guid PaperId,
    ExamMode Mode,
    Guid? ReplaceSessionId = null) : IRequest<StartSessionResultDto>;
