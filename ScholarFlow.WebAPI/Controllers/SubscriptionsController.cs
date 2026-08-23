using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.Subscriptions.Queries.GetMySubscriptionStatus;
using ScholarFlow.Modules.Subscriptions.Queries.GetSubscriptionPlans;
using ScholarFlow.Modules.Subscriptions.Queries.GetLaunchOfferStatus;
using ScholarFlow.Modules.Subscriptions.Commands.ClaimLaunchOffer;

namespace ScholarFlow.WebAPI.Controllers;

[ApiController]
[Route("api/subscriptions")]
public sealed class SubscriptionsController(IMediator mediator) : ControllerBase
{
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans(CancellationToken ct)
        => Ok(await mediator.Send(new GetSubscriptionPlansQuery(), ct));

    [HttpGet("me")]
    [Authorize(Roles = AppRole.Student)]
    public async Task<IActionResult> GetMyStatus(CancellationToken ct)
        => Ok(await mediator.Send(new GetMySubscriptionStatusQuery(), ct));

    [HttpGet("launch-offer")]
    [Authorize(Roles = AppRole.Student)]
    public async Task<IActionResult> GetLaunchOffer(CancellationToken ct)
        => Ok(await mediator.Send(new GetLaunchOfferStatusQuery(), ct));

    [HttpPost("launch-offer/claim")]
    [Authorize(Roles = AppRole.Student)]
    public async Task<IActionResult> ClaimLaunchOffer(CancellationToken ct)
        => Ok(await mediator.Send(new ClaimLaunchOfferCommand(), ct));
}
