using MediatR;
using ScholarFlow.Modules.Academic.DTOs;

namespace ScholarFlow.Modules.Academic.Queries.GetAcademicTree;

public sealed record GetAcademicTreeQuery : IRequest<List<AcademicStreamTreeDto>>;
