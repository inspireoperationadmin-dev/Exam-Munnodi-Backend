using MediatR;
using Microsoft.AspNetCore.Identity;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.Modules.Subscriptions.DTOs;
using ScholarFlow.SharedKernel.Exceptions;
using ScholarFlow.SharedKernel.IntegrationEvents;

namespace ScholarFlow.Modules.Subscriptions.Commands.ActivateStudentSubscription;

public sealed class ActivateStudentSubscriptionCommandHandler(
    ISubscriptionRepository subscriptionRepo,
    ISubscriptionsApi subscriptionsApi,
    UserManager<ApplicationUser> userManager,
    ICurrentUser currentUser,
    IMediator mediator)
    : IRequestHandler<ActivateStudentSubscriptionCommand, SubscriptionStatusDto>
{
    public async Task<SubscriptionStatusDto> Handle(ActivateStudentSubscriptionCommand request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new NotFoundException("Student not found.");

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains(AppRole.Student))
            throw new BadRequestException("Subscriptions can only be assigned to student accounts.");

        var plan = await subscriptionRepo.GetPlanByCodeAsync(request.PlanCode, ct)
            ?? throw new NotFoundException("Subscription plan not found.");

        if (!plan.IsActive)
            throw new BadRequestException("Subscription plan is not active.");

        if (plan.Tier == SubscriptionTier.Free || plan.PriceLkr <= 0)
            throw new BadRequestException("The admin activation endpoint only accepts paid Basic or Pro plans.");

        if (plan.BillingCycle == SubscriptionBillingCycle.Annual && request.BillingPeriods != 1)
            throw new BadRequestException("Annual plans must be paid and activated as one full annual period.");

        var expectedAmount = plan.PriceLkr * request.BillingPeriods;
        if (request.AmountLkr != expectedAmount)
            throw new BadRequestException(
                $"The payment amount must be LKR {expectedAmount:0.00} for the selected plan and period count.");

        var now = DateTime.UtcNow;
        var requestedStart = request.StartsAt?.ToUniversalTime() ?? now;
        var durationDays = checked(plan.DurationDays * request.BillingPeriods);

        var activeSubscriptions = await subscriptionRepo.GetActiveSubscriptionsAsync(request.UserId, ct);
        var remainingSubscriptions = activeSubscriptions
            .Where(s => s.EndsAt > now)
            .ToList();
        var samePlanSubscriptions = remainingSubscriptions
            .Where(s => s.PlanId == plan.Id)
            .ToList();
        DateTime start;
        if (remainingSubscriptions.Count == 0)
        {
            start = requestedStart;
        }
        else if (samePlanSubscriptions.Count == remainingSubscriptions.Count)
        {
            // Continue the current chain and fill any gap left by a cancelled renewal.
            start = GetRenewalStart(samePlanSubscriptions, now);
        }
        else
        {
            // A plan change is queued after every current or scheduled period.
            // Activating a new plan must never cancel access that has already been paid for.
            start = remainingSubscriptions.Max(subscription => subscription.EndsAt);
        }

        var end = start.AddDays(durationDays);
        var periodNote = request.BillingPeriods == 1
            ? request.AdminNote
            : $"{request.BillingPeriods} monthly periods purchased. {request.AdminNote}".Trim();

        var subscription = StudentSubscription.Activate(
            userId: request.UserId,
            planId: plan.Id,
            startsAt: start,
            endsAt: end,
            activatedByAdminId: currentUser.UserId,
            notes: periodNote);

        await subscriptionRepo.AddSubscriptionAsync(subscription, ct);

        await subscriptionRepo.AddPaymentAsync(SubscriptionPayment.ApprovedBankTransfer(
            userId: request.UserId,
            subscriptionId: subscription.Id,
            amountLkr: request.AmountLkr.Value,
            referenceNumber: request.PaymentReference,
            receiptImageUrl: request.ReceiptImageUrl,
            adminNote: periodNote,
            reviewedByAdminId: currentUser.UserId), ct);

        await subscriptionRepo.SaveChangesAsync(ct);

        await mediator.Publish(new StudentSubscriptionActivatedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow,
            UserId: request.UserId,
            PlanName: plan.Name,
            StartsAt: start,
            EndsAt: end,
            IsScheduled: start > now), CancellationToken.None);

        var summary = await subscriptionsApi.GetAccessSummaryAsync(request.UserId, ct);

        return SubscriptionStatusDto.From(summary);
    }

    private static DateTime GetRenewalStart(
        IReadOnlyCollection<StudentSubscription> subscriptions,
        DateTime now)
    {
        var currentPeriods = subscriptions
            .Where(subscription => subscription.StartsAt <= now && subscription.EndsAt > now)
            .ToList();

        if (currentPeriods.Count == 0)
            return subscriptions.Max(subscription => subscription.EndsAt);

        var accessEnd = currentPeriods.Max(subscription => subscription.EndsAt);
        foreach (var period in subscriptions
                     .Where(subscription => subscription.StartsAt > now)
                     .OrderBy(subscription => subscription.StartsAt))
        {
            if (period.StartsAt > accessEnd) break;
            if (period.EndsAt > accessEnd) accessEnd = period.EndsAt;
        }

        return accessEnd;
    }
}
