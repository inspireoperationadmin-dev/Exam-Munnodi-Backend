using MediatR;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Queries.GetSessionReview;

public sealed record GetSessionReviewQuery(Guid SessionId) : IRequest<List<SessionReviewItemDto>>;
