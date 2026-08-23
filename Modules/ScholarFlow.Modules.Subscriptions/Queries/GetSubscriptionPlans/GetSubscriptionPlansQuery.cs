using MediatR;
using ScholarFlow.Modules.Subscriptions.DTOs;

namespace ScholarFlow.Modules.Subscriptions.Queries.GetSubscriptionPlans;

public sealed record GetSubscriptionPlansQuery : IRequest<List<SubscriptionPlanDto>>;
