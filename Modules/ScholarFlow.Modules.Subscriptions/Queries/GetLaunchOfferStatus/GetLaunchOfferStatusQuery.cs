using MediatR;
using ScholarFlow.Modules.Subscriptions.DTOs;

namespace ScholarFlow.Modules.Subscriptions.Queries.GetLaunchOfferStatus;

public sealed record GetLaunchOfferStatusQuery : IRequest<LaunchOfferStatusDto>;
