using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Subscriptions.DTOs;

namespace ScholarFlow.Modules.Subscriptions.Queries.Admin.GetAdminStudentSubscriptionPeriods;

public sealed class GetAdminStudentSubscriptionPeriodsQueryHandler(ISqlConnectionFactory sql)
    : IRequestHandler<GetAdminStudentSubscriptionPeriodsQuery, List<AdminSubscriptionPeriodDto>>
{
    public async Task<List<AdminSubscriptionPeriodDto>> Handle(
        GetAdminStudentSubscriptionPeriodsQuery request,
        CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var periods = await conn.QueryAsync<AdminSubscriptionPeriodDto>("""
            SELECT
                ss.Id AS SubscriptionId,
                sp.Code AS PlanCode,
                sp.Name AS PlanName,
                sp.Tier,
                sp.BillingCycle,
                CASE
                    WHEN ss.StartsAt > SYSUTCDATETIME() THEN N'Scheduled'
                    ELSE N'Current'
                END AS PeriodState,
                ss.StartsAt,
                ss.EndsAt,
                ss.ActivationSource,
                payment.AmountLkr AS PaymentAmountLkr,
                payment.ReferenceNumber AS PaymentReference,
                payment.ReviewedAt AS PaymentReviewedAt
            FROM StudentSubscriptions ss
            JOIN SubscriptionPlans sp ON sp.Id = ss.PlanId
            OUTER APPLY (
                SELECT TOP 1
                    p.AmountLkr,
                    p.ReferenceNumber,
                    p.ReviewedAt
                FROM SubscriptionPayments p
                WHERE p.SubscriptionId = ss.Id
                  AND p.IsDeleted = 0
                ORDER BY p.ReviewedAt DESC
            ) payment
            WHERE ss.UserId = @UserId
              AND ss.Status = N'Active'
              AND ss.EndsAt > SYSUTCDATETIME()
              AND ss.IsDeleted = 0
              AND ss.ActivationSource = N'AdminManual'
              AND sp.Tier <> N'Free'
            ORDER BY ss.StartsAt, ss.EndsAt
            """, new { request.UserId });

        return periods.ToList();
    }
}
