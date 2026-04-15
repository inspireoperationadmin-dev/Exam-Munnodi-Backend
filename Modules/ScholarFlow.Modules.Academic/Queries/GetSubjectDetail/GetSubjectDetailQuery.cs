using MediatR;
using ScholarFlow.Modules.Academic.DTOs;

namespace ScholarFlow.Modules.Academic.Queries.GetSubjectDetail;

public sealed record GetSubjectDetailQuery(Guid Id) : IRequest<SubjectDetailDto>;
