using MediatR;
using ScholarFlow.Modules.Academic.DTOs;

namespace ScholarFlow.Modules.Academic.Queries.GetPaperDetail;

public sealed record GetPaperDetailQuery(Guid Id) : IRequest<PaperDetailDto>;
