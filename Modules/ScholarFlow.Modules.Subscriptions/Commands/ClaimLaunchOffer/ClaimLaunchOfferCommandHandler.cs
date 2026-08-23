using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.Modules.Subscriptions.DTOs;
using ScholarFlow.Modules.Subscriptions.Settings;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Subscriptions.Commands.ClaimLaunchOffer;

public sealed class ClaimLaunchOfferCommandHandler(
    IApplicationDbContext db,
    ISubscriptionRepository subscriptionRepo,
    ISubscriptionsApi subscriptionsApi,
    ICurrentUser currentUser,
    IOptions<LaunchOfferSettings> settings)
    : IRequestHandler<ClaimLaunchOfferCommand, SubscriptionStatusDto>
{
    public async Task<SubscriptionStatusDto> Handle(ClaimLaunchOfferCommand request, CancellationToken ct)
    {
        var offer = settings.Value;
        var now = DateTime.UtcNow;

        if (!offer.IsClaimWindowOpen(now))
            throw new ForbiddenException("The launch offer is not currently available.");

        var setupComplete = await db.StudentProfiles
            .AsNoTracking()
            .AnyAsync(p => p.UserId == currentUser.UserId
                        && p.User.IsProfileSetup
                        && p.SubjectSelections.Count == 3,
                ct);

        if (!setupComplete)
            throw new ForbiddenException("Complete your profile and select three subjects before claiming the launch offer.");

        if (await subscriptionRepo.HasPromotionAsync(currentUser.UserId, offer.PromotionCode, ct))
            return await GetStatusAsync(ct);

        var currentPaidSubscription = await subscriptionRepo.GetCurrentActiveAsync(currentUser.UserId, ct);
        if (currentPaidSubscription is not null)
            throw new ConflictException("An active paid subscription already exists for this account.");

        var plan = await subscriptionRepo.GetPlanByCodeAsync(SubscriptionPlanCode.BasicMonthly, ct)
            ?? throw new NotFoundException("Basic monthly subscription plan not found.");

        if (!plan.IsActive || plan.Tier != SubscriptionTier.Basic)
            throw new BadRequestException("The launch offer plan is not available.");

        var subscription = StudentSubscription.Activate(
            userId: currentUser.UserId,
            planId: plan.Id,
            startsAt: now,
            endsAt: now.AddDays(offer.DurationDays),
            activatedByAdminId: null,
            notes: "One-time Basic access claimed through the initial launch offer.",
            activationSource: SubscriptionActivationSource.LaunchPromotion,
            promotionCode: offer.PromotionCode);

        await subscriptionRepo.TryAddPromotionSubscriptionAsync(subscription, ct);
        return await GetStatusAsync(ct);
    }

    private async Task<SubscriptionStatusDto> GetStatusAsync(CancellationToken ct)
    {
        var summary = await subscriptionsApi.GetAccessSummaryAsync(currentUser.UserId, ct);
        return SubscriptionStatusDto.From(summary);
    }
}
