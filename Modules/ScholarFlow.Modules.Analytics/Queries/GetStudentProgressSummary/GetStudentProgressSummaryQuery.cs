using MediatR;
using ScholarFlow.Modules.Analytics.DTOs;

namespace ScholarFlow.Modules.Analytics.Queries.GetStudentProgressSummary;

public sealed record GetStudentProgressSummaryQuery(Guid? SubjectId = null)
    : IRequest<StudentProgressSummaryDto>;
