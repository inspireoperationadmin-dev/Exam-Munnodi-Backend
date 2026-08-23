using MediatR;
using ScholarFlow.Modules.Subscriptions.DTOs;

namespace ScholarFlow.Modules.Subscriptions.Commands.ClaimLaunchOffer;

public sealed record ClaimLaunchOfferCommand : IRequest<SubscriptionStatusDto>;
