using MediatR;
using ScholarFlow.Modules.Analytics.DTOs;

namespace ScholarFlow.Modules.Analytics.Queries.GetSubjectPerformance;

public sealed record GetSubjectPerformanceQuery : IRequest<List<SubjectPerformanceDto>>;
