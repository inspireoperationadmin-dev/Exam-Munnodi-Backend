using MediatR;
using ScholarFlow.Modules.Analytics.DTOs.Admin;

namespace ScholarFlow.Modules.Analytics.Queries.Admin.GetAdminStudentProgress;

public sealed record GetAdminStudentProgressQuery : IRequest<IReadOnlyList<AdminStudentProgressSummaryDto>>;
