using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Modules.Subscriptions.Commands.ActivateStudentSubscription;
using ScholarFlow.Modules.Subscriptions.Commands.CancelStudentSubscription;
using ScholarFlow.Modules.Subscriptions.Commands.UpdateSubscriptionPlanPricing;
using ScholarFlow.Modules.Subscriptions.Queries.Admin.GetAdminStudentSubscription;
using ScholarFlow.Modules.Subscriptions.Queries.Admin.GetAdminStudentSubscriptionPeriods;

namespace ScholarFlow.WebAPI.Controllers;

[ApiController]
[Route("api/admin/subscriptions")]
[Authorize(Roles = AppRole.Admin + "," + AppRole.SuperAdmin)]
public sealed class AdminSubscriptionsController(IMediator mediator) : ControllerBase
{
    [HttpGet("students/{studentId:guid}")]
    public async Task<IActionResult> GetStudentSubscription(Guid studentId, CancellationToken ct)
        => Ok(await mediator.Send(new GetAdminStudentSubscriptionQuery(studentId), ct));

    [HttpGet("students/{studentId:guid}/periods")]
    public async Task<IActionResult> GetStudentSubscriptionPeriods(Guid studentId, CancellationToken ct)
        => Ok(await mediator.Send(new GetAdminStudentSubscriptionPeriodsQuery(studentId), ct));

    [HttpPost("students/{studentId:guid}/activate")]
    public async Task<IActionResult> Activate(
        Guid studentId,
        [FromBody] ActivateStudentSubscriptionRequest request,
        CancellationToken ct)
        => Ok(await mediator.Send(new ActivateStudentSubscriptionCommand(
            UserId: studentId,
            PlanCode: request.PlanCode,
            BillingPeriods: request.BillingPeriods,
            AmountLkr: request.AmountLkr,
            PaymentReference: request.PaymentReference,
            ReceiptImageUrl: request.ReceiptImageUrl,
            AdminNote: request.AdminNote,
            StartsAt: request.StartsAt), ct));

    [HttpPost("students/{studentId:guid}/subscriptions/{subscriptionId:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid studentId,
        Guid subscriptionId,
        [FromBody] CancelStudentSubscriptionRequest? request,
        CancellationToken ct)
    {
        await mediator.Send(new CancelStudentSubscriptionCommand(
            studentId,
            subscriptionId,
            request?.AdminNote), ct);
        return Ok();
    }

    [HttpPatch("plans/{planCode}")]
    public async Task<IActionResult> UpdatePlanPricing(
        SubscriptionPlanCode planCode,
        [FromBody] UpdateSubscriptionPlanPricingRequest request,
        CancellationToken ct)
        => Ok(await mediator.Send(new UpdateSubscriptionPlanPricingCommand(
            planCode,
            request.BasePriceLkr,
            request.DiscountPercentage,
            request.AdminNote), ct));
}

public sealed record ActivateStudentSubscriptionRequest(
    SubscriptionPlanCode PlanCode,
    int BillingPeriods,
    decimal? AmountLkr,
    string? PaymentReference,
    string? ReceiptImageUrl,
    string? AdminNote,
    DateTime? StartsAt);

public sealed record CancelStudentSubscriptionRequest(string? AdminNote);

public sealed record UpdateSubscriptionPlanPricingRequest(
    decimal BasePriceLkr,
    decimal DiscountPercentage,
    string? AdminNote);
