using MediatR;
using ScholarFlow.Modules.Analytics.DTOs;

namespace ScholarFlow.Modules.Analytics.Queries.GetTopicPerformance;

public sealed record GetTopicPerformanceQuery(Guid SubjectId) : IRequest<List<TopicPerformanceDto>>;
