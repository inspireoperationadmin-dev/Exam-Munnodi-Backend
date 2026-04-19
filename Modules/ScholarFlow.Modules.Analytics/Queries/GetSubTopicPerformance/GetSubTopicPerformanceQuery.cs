using MediatR;
using ScholarFlow.Modules.Analytics.DTOs;

namespace ScholarFlow.Modules.Analytics.Queries.GetSubTopicPerformance;

public sealed record GetSubTopicPerformanceQuery(Guid SubjectId) : IRequest<List<SubTopicPerformanceDto>>;
