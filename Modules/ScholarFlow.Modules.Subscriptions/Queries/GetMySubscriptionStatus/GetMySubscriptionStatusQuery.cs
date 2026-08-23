using MediatR;
using ScholarFlow.Modules.Subscriptions.DTOs;

namespace ScholarFlow.Modules.Subscriptions.Queries.GetMySubscriptionStatus;

public sealed record GetMySubscriptionStatusQuery : IRequest<SubscriptionStatusDto>;
