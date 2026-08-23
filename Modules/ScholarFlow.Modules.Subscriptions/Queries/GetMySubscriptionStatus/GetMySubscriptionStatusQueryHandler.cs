using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Subscriptions.DTOs;

namespace ScholarFlow.Modules.Subscriptions.Queries.GetMySubscriptionStatus;

public sealed class GetMySubscriptionStatusQueryHandler(
    ISubscriptionsApi subscriptionsApi,
    ICurrentUser currentUser)
    : IRequestHandler<GetMySubscriptionStatusQuery, SubscriptionStatusDto>
{
    public async Task<SubscriptionStatusDto> Handle(GetMySubscriptionStatusQuery request, CancellationToken ct)
    {
        var summary = await subscriptionsApi.GetAccessSummaryAsync(currentUser.UserId, ct);

        return SubscriptionStatusDto.From(summary);
    }
}
