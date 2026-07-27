using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Queries.GetMySessions;

public sealed record GetMySessionsQuery(
    Guid? PaperId,
    ExamMode? Mode) : IRequest<List<SessionSummaryDto>>;
