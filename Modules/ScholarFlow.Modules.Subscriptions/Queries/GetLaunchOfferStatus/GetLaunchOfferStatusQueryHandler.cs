using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.Modules.Subscriptions.DTOs;
using ScholarFlow.Modules.Subscriptions.Settings;

namespace ScholarFlow.Modules.Subscriptions.Queries.GetLaunchOfferStatus;

public sealed class GetLaunchOfferStatusQueryHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    ISubscriptionRepository subscriptionRepo,
    IOptions<LaunchOfferSettings> settings)
    : IRequestHandler<GetLaunchOfferStatusQuery, LaunchOfferStatusDto>
{
    public async Task<LaunchOfferStatusDto> Handle(GetLaunchOfferStatusQuery request, CancellationToken ct)
    {
        var offer = settings.Value;
        var now = DateTime.UtcNow;

        var claimed = await db.StudentSubscriptions
            .AsNoTracking()
            .Where(s => s.UserId == currentUser.UserId
                     && s.PromotionCode == offer.PromotionCode)
            .OrderByDescending(s => s.ActivatedAt)
            .Select(s => new { s.ActivatedAt, s.EndsAt })
            .FirstOrDefaultAsync(ct);

        var setup = await db.StudentProfiles
            .AsNoTracking()
            .Where(p => p.UserId == currentUser.UserId)
            .Select(p => new
            {
                p.User.IsProfileSetup,
                SubjectCount = p.SubjectSelections.Count
            })
            .FirstOrDefaultAsync(ct);

        var windowOpen = offer.IsClaimWindowOpen(now);
        var setupComplete = setup is { IsProfileSetup: true, SubjectCount: 3 };
        var hasActivePaidSubscription = await subscriptionRepo.GetCurrentActiveAsync(
            currentUser.UserId,
            ct) is not null;
        var eligible = windowOpen
            && setupComplete
            && claimed is null
            && !hasActivePaidSubscription;

        var status = claimed is not null
            ? "Claimed"
            : !offer.Enabled
                ? "Disabled"
                : !windowOpen
                    ? "OutsideClaimWindow"
                    : !setupComplete
                        ? "ProfileSetupRequired"
                        : hasActivePaidSubscription
                            ? "ActiveSubscription"
                            : "Eligible";

        return new LaunchOfferStatusDto(
            offer.Enabled,
            eligible,
            claimed is not null,
            status,
            offer.PromotionCode,
            offer.DurationDays,
            offer.ClaimStartsAtUtc,
            offer.ClaimEndsAtUtc,
            claimed?.ActivatedAt,
            claimed?.EndsAt);
    }
}
