using MediatR;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Commands.EndExamSession;

public sealed record EndExamSessionCommand(Guid SessionId) : IRequest<EndSessionResultDto>;
