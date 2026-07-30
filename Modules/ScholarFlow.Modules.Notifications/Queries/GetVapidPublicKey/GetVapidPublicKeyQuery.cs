using MediatR;
using ScholarFlow.Modules.Notifications.DTOs;

namespace ScholarFlow.Modules.Notifications.Queries.GetVapidPublicKey;

public sealed record GetVapidPublicKeyQuery : IRequest<VapidPublicKeyDto>;
