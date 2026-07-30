using MediatR;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Queries.GetActiveSession;

public sealed record GetActiveSessionQuery(Guid? SubjectId = null) : IRequest<ActiveSessionDto?>;
