using MediatR;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Queries.GetMySessions;

public sealed record GetMySessionsQuery(
    Guid? PaperId,
    bool? IsPractice) : IRequest<List<SessionSummaryDto>>;
