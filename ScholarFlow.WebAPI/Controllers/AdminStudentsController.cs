using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.Identity.Commands.UpdateStudentAccountAccess;
using ScholarFlow.Modules.Identity.Commands.DeleteStudentAccount;

namespace ScholarFlow.WebAPI.Controllers;

[ApiController]
[Route("api/admin/students")]
[Authorize(Roles = AppRole.SuperAdmin)]
public sealed class AdminStudentsController(IMediator mediator) : ControllerBase
{
    [HttpDelete("{studentId:guid}")]
    public async Task<IActionResult> DeleteStudentAccount(
        Guid studentId,
        [FromBody] DeleteStudentAccountRequest request,
        CancellationToken ct)
        => Ok(await mediator.Send(new DeleteStudentAccountCommand(
            studentId,
            request.ConfirmationEmail), ct));

    [HttpPut("{studentId:guid}/account-access")]
    public async Task<IActionResult> UpdateAccountAccess(
        Guid studentId,
        [FromBody] UpdateStudentAccountAccessRequest request,
        CancellationToken ct)
        => Ok(await mediator.Send(new UpdateStudentAccountAccessCommand(
            studentId,
            request.Action,
            request.SuspendedUntil), ct));
}

public sealed record UpdateStudentAccountAccessRequest(
    StudentAccountAccessAction Action,
    DateTimeOffset? SuspendedUntil);

public sealed record DeleteStudentAccountRequest(string ConfirmationEmail);
