using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarFlow.Modules.Notifications.Commands.DeactivateNotificationDevice;
using ScholarFlow.Modules.Notifications.Commands.RegisterNotificationDevice;
using ScholarFlow.Modules.Notifications.Commands.UpdateNotificationPreferences;
using ScholarFlow.Modules.Notifications.Queries.GetMyNotificationDevices;
using ScholarFlow.Modules.Notifications.Queries.GetNotificationPreferences;
using ScholarFlow.Modules.Notifications.Queries.GetVapidPublicKey;

namespace ScholarFlow.WebAPI.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(IMediator mediator) : ControllerBase
{
    [HttpGet("vapid-public-key")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVapidPublicKey(CancellationToken ct)
        => Ok(await mediator.Send(new GetVapidPublicKeyQuery(), ct));

    [HttpPost("devices")]
    [Authorize]
    public async Task<IActionResult> RegisterDevice(
        [FromBody] RegisterNotificationDeviceCommand command,
        CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpGet("devices/me")]
    [Authorize]
    public async Task<IActionResult> GetMyDevices(CancellationToken ct)
        => Ok(await mediator.Send(new GetMyNotificationDevicesQuery(), ct));

    [HttpGet("preferences")]
    [Authorize]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
        => Ok(await mediator.Send(new GetNotificationPreferencesQuery(), ct));

    [HttpPut("preferences")]
    [Authorize]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateNotificationPreferencesCommand command,
        CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpDelete("devices/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeactivateDevice(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateNotificationDeviceCommand(id), ct);
        return Ok();
    }
}
