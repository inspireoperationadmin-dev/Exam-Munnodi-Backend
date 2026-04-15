using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.Academic.DTOs;

namespace ScholarFlow.Modules.Academic.Queries.GetPapers;

public sealed record GetPapersQuery(
    Guid? SubjectId,
    PaperType? Type,
    PaperMedium? Medium,
    int? Year) : IRequest<List<PaperSummaryDto>>;
