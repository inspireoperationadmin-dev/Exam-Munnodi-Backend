using MediatR;
using ScholarFlow.Modules.Academic.DTOs;

namespace ScholarFlow.Modules.Academic.Queries.GetAllSubjects;

public sealed record GetAllSubjectsQuery(Guid? StreamId) : IRequest<List<SubjectDetailDto>>;