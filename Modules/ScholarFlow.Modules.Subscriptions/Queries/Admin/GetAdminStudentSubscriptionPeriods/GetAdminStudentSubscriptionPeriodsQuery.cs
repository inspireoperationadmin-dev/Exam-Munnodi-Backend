using MediatR;
using ScholarFlow.Modules.Subscriptions.DTOs;

namespace ScholarFlow.Modules.Subscriptions.Queries.Admin.GetAdminStudentSubscriptionPeriods;

public sealed record GetAdminStudentSubscriptionPeriodsQuery(Guid UserId)
    : IRequest<List<AdminSubscriptionPeriodDto>>;
