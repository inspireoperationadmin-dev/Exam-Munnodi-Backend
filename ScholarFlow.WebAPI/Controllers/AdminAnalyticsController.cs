using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.Analytics.Queries.Admin.GetAdminStudentProgress;
using ScholarFlow.Modules.Analytics.Queries.Admin.GetAdminStudentProgressDetail;

namespace ScholarFlow.WebAPI.Controllers;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = AppRole.Admin + "," + AppRole.SuperAdmin)]
public sealed class AdminAnalyticsController(IMediator mediator) : ControllerBase
{
    [HttpGet("students")]
    public async Task<IActionResult> GetStudentProgress(
        [FromQuery] AdminStudentStatusFilter? status,
        CancellationToken ct)
        => Ok(await mediator.Send(new GetAdminStudentProgressQuery(status), ct));

    [HttpGet("students/{studentId:guid}")]
    public async Task<IActionResult> GetStudentProgressDetail(Guid studentId, CancellationToken ct)
        => Ok(await mediator.Send(new GetAdminStudentProgressDetailQuery(studentId), ct));
}
