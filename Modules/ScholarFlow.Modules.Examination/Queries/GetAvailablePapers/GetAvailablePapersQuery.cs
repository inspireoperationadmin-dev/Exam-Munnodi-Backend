using MediatR;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Queries.GetAvailablePapers;

public sealed record GetAvailablePapersQuery(
    Guid? SubjectId,
    string? Type,
    string? Medium,
    int? Year) : IRequest<List<AvailablePaperDto>>;
