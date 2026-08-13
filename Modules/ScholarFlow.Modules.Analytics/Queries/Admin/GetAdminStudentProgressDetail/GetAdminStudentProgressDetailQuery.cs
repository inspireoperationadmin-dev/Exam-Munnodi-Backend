using MediatR;
using ScholarFlow.Modules.Analytics.DTOs.Admin;

namespace ScholarFlow.Modules.Analytics.Queries.Admin.GetAdminStudentProgressDetail;

public sealed record GetAdminStudentProgressDetailQuery(Guid StudentId) : IRequest<AdminStudentProgressDetailDto>;
