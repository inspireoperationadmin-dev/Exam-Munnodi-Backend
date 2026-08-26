using MediatR;
using ScholarFlow.Modules.Analytics.DTOs.Admin;

namespace ScholarFlow.Modules.Analytics.Queries.Admin.GetAdminStudentProgress;

public enum AdminStudentStatusFilter
{
    Active,
    Inactive,
    NotStarted,
    Improving,
    Stable,
    Decreasing,
    SetupOnly
}

public sealed record GetAdminStudentProgressQuery(AdminStudentStatusFilter? Status = null)
    : IRequest<IReadOnlyList<AdminStudentProgressSummaryDto>>;
