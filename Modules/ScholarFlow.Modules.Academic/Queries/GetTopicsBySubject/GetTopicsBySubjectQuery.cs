using MediatR;
using ScholarFlow.Modules.Academic.DTOs;

namespace ScholarFlow.Modules.Academic.Queries.GetTopicsBySubject;

public sealed record GetTopicsBySubjectQuery(Guid SubjectId) : IRequest<List<TopicWithSubTopicsDto>>;
