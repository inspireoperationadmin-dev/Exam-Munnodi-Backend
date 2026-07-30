using MediatR;

namespace ScholarFlow.Modules.Examination.Commands.AbandonExamSession;

public sealed record AbandonExamSessionCommand(Guid SessionId) : IRequest;
