using MediatR;
using Microsoft.AspNetCore.Mvc;
using ScholarFlow.Modules.Identity.Commands.Login;
using ScholarFlow.Modules.Identity.Commands.Register;
using ScholarFlow.Modules.Identity.Commands.SendOtp;
using ScholarFlow.Modules.Identity.Commands.VerifyOtp;

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

    [HttpPost("otp/send")]
    public async Task<IActionResult> SendOtp(
        [FromBody] SendOtpCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return Ok(new { message = "OTP sent successfully." });
    }

    [HttpPost("otp/verify")]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyOtpCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct); // returns AuthResponse
        return Ok(result);
    }
}