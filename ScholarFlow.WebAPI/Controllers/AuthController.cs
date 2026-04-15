using MediatR;
using Microsoft.AspNetCore.Mvc;
using ScholarFlow.Modules.Identity.Commands.Login;
using ScholarFlow.Modules.Identity.Commands.Register;

namespace ScholarFlow.WebAPI.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));
}
