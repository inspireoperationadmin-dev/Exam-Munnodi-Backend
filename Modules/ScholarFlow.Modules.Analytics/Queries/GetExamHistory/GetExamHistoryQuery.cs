using MediatR;
using ScholarFlow.Modules.Analytics.DTOs;

namespace ScholarFlow.Modules.Analytics.Queries.GetExamHistory;

public sealed record GetExamHistoryQuery(Guid SubjectId) : IRequest<List<ExamHistoryDto>>;
