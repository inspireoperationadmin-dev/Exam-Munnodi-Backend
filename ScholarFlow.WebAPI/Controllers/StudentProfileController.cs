using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.UserProfiles.Commands.SetupStudentProfile;
using ScholarFlow.Modules.UserProfiles.Queries.GetStudentProfile;
using ScholarFlow.Modules.UserProfiles.Queries.GetStudentProfileSummary;

namespace ScholarFlow.WebAPI.Controllers;

[ApiController]
[Route("api/userprofiles/student")]
[Authorize(Roles = AppRole.Student)]
public sealed class StudentProfileController(IMediator mediator) : ControllerBase
{
    // GET /api/userprofiles/student/profile
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
        => Ok(await mediator.Send(new GetStudentProfileQuery(), ct));

    // GET /api/userprofiles/student/profile-summary
    [HttpGet("profile-summary")]
    public async Task<IActionResult> GetProfileSummary(CancellationToken ct)
        => Ok(await mediator.Send(new GetStudentProfileSummaryQuery(), ct));

    // POST /api/userprofiles/student/profile/setup
    [HttpPost("profile/setup")]
    public async Task<IActionResult> SetupProfile(
        [FromBody] SetupStudentProfileCommand command,
        CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return Ok();
    }
}
