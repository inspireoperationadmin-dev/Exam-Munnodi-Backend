using MediatR;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Queries.GetSessionResume;

public sealed record GetSessionResumeQuery(Guid SessionId) : IRequest<ResumeSessionResultDto>;
