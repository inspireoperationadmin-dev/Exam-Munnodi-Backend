using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Queries.GetMySessions;

public sealed record GetMySessionsQuery(
    Guid? PaperId,
    Guid? SubjectId,
    ExamMode? Mode) : IRequest<List<SessionSummaryDto>>;
