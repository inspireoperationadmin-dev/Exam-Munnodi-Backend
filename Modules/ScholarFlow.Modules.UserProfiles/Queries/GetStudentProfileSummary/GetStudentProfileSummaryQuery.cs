using MediatR;
using ScholarFlow.Modules.UserProfiles.DTOs;

namespace ScholarFlow.Modules.UserProfiles.Queries.GetStudentProfileSummary;

public sealed record GetStudentProfileSummaryQuery : IRequest<StudentProfileSummaryDto>;