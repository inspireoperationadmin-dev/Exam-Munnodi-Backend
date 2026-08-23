using MediatR;
using ScholarFlow.Modules.Subscriptions.DTOs;

namespace ScholarFlow.Modules.Subscriptions.Queries.Admin.GetAdminStudentSubscription;

public sealed record GetAdminStudentSubscriptionQuery(Guid UserId) : IRequest<AdminStudentSubscriptionDto>;
