using MediatR;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Queries.GetSessionDetail;

public sealed record GetSessionDetailQuery(Guid SessionId) : IRequest<SessionDetailDto>;
