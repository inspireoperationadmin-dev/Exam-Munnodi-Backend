using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Subscriptions.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Subscriptions.Queries.Admin.GetAdminStudentSubscription;

public sealed class GetAdminStudentSubscriptionQueryHandler(ISqlConnectionFactory sql)
    : IRequestHandler<GetAdminStudentSubscriptionQuery, AdminStudentSubscriptionDto>
{
    public async Task<AdminStudentSubscriptionDto> Handle(GetAdminStudentSubscriptionQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var row = await conn.QueryFirstOrDefaultAsync<AdminStudentSubscriptionDto>("""
            WITH CurrentSubscription AS (
                SELECT TOP 1
                    ss.UserId,
                    ss.StartsAt,
                    ss.EndsAt,
                    ss.Status,
                    ss.ActivationSource,
                    sp.Code AS PlanCode,
                    sp.Name AS PlanName,
                    sp.Tier,
                    sp.BillingCycle,
                    sp.ProgressAccessLevel,
                    sp.AllowsPaperExamMode,
                    sp.FreePastPaperCount,
                    sp.FreeModelPaperCount,
                    sp.MonthlyMockExamLimit,
                    sp.MonthlyUnitExamLimit
                FROM StudentSubscriptions ss
                JOIN SubscriptionPlans sp ON sp.Id = ss.PlanId
                WHERE ss.UserId = @UserId
                  AND ss.Status = N'Active'
                  AND ss.IsDeleted = 0
                  AND ss.StartsAt <= SYSUTCDATETIME()
                  AND ss.EndsAt > SYSUTCDATETIME()
                  AND sp.IsActive = 1
                  AND sp.Tier <> N'Free'
                ORDER BY sp.Tier DESC, ss.EndsAt DESC
            ),
            FreePlan AS (
                SELECT TOP 1
                    Code AS PlanCode,
                    Name AS PlanName,
                    Tier,
                    BillingCycle,
                    ProgressAccessLevel,
                    AllowsPaperExamMode,
                    FreePastPaperCount,
                    FreeModelPaperCount,
                    MonthlyMockExamLimit,
                    MonthlyUnitExamLimit
                FROM SubscriptionPlans
                WHERE Code = N'Free'
                  AND IsActive = 1
                  AND IsDeleted = 0
            ),
            CurrentUsage AS (
                SELECT
                    UserId,
                    SUM(CASE WHEN Feature = N'MockExam' THEN UsedCount ELSE 0 END) AS MockExamsUsed,
                    SUM(CASE WHEN Feature = N'UnitExam' THEN UsedCount ELSE 0 END) AS UnitExamsUsed
                FROM StudentSubscriptionUsages
                WHERE UserId = @UserId
                  AND PeriodStart = DATEFROMPARTS(
                      YEAR(DATEADD(minute, 330, SYSUTCDATETIME())),
                      MONTH(DATEADD(minute, 330, SYSUTCDATETIME())),
                      1)
                  AND IsDeleted = 0
                GROUP BY UserId
            ),
            LastPayment AS (
                SELECT TOP 1
                    UserId,
                    AmountLkr AS LastPaymentAmountLkr,
                    ReferenceNumber AS LastPaymentReference,
                    ReviewedAt AS LastPaymentReviewedAt
                FROM SubscriptionPayments
                WHERE UserId = @UserId
                  AND IsDeleted = 0
                ORDER BY ReviewedAt DESC
            ),
            LaunchPromotion AS (
                SELECT TOP 1
                    UserId,
                    PromotionCode,
                    ActivatedAt AS PromotionClaimedAt,
                    EndsAt AS PromotionAccessEndedAt
                FROM StudentSubscriptions
                WHERE UserId = @UserId
                  AND PromotionCode IS NOT NULL
                  AND IsDeleted = 0
                ORDER BY ActivatedAt DESC
            )
            SELECT
                u.Id AS UserId,
                u.Email,
                pr.FullName,
                CAST(1 AS bit) AS HasActiveAccess,
                CASE
                    WHEN cs.UserId IS NULL THEN N'Free'
                    ELSE N'Active'
                END AS Status,
                COALESCE(cs.PlanCode, N'Free') AS PlanCode,
                COALESCE(cs.PlanName, N'Free') AS PlanName,
                COALESCE(cs.Tier, fp.Tier) AS Tier,
                COALESCE(cs.BillingCycle, fp.BillingCycle) AS BillingCycle,
                COALESCE(cs.ProgressAccessLevel, fp.ProgressAccessLevel) AS ProgressAccessLevel,
                CASE WHEN cs.UserId IS NOT NULL THEN cs.AllowsPaperExamMode ELSE fp.AllowsPaperExamMode END AS AllowsPaperExamMode,
                CASE WHEN cs.UserId IS NOT NULL THEN cs.FreePastPaperCount ELSE fp.FreePastPaperCount END AS FreePastPaperCount,
                CASE WHEN cs.UserId IS NOT NULL THEN cs.FreeModelPaperCount ELSE fp.FreeModelPaperCount END AS FreeModelPaperCount,
                CASE WHEN cs.UserId IS NOT NULL THEN cs.MonthlyMockExamLimit ELSE fp.MonthlyMockExamLimit END AS MonthlyMockExamLimit,
                CASE WHEN cs.UserId IS NOT NULL THEN cs.MonthlyUnitExamLimit ELSE fp.MonthlyUnitExamLimit END AS MonthlyUnitExamLimit,
                COALESCE(usage.MockExamsUsed, 0) AS MockExamsUsed,
                COALESCE(usage.UnitExamsUsed, 0) AS UnitExamsUsed,
                cs.ActivationSource,
                cs.StartsAt,
                cs.EndsAt,
                CASE
                    WHEN cs.UserId IS NOT NULL
                    THEN DATEDIFF(day, SYSUTCDATETIME(), cs.EndsAt) + 1
                    ELSE NULL
                END AS DaysRemaining,
                lp.LastPaymentAmountLkr,
                lp.LastPaymentReference,
                lp.LastPaymentReviewedAt,
                CAST(CASE WHEN promo.UserId IS NULL THEN 0 ELSE 1 END AS bit) AS LaunchOfferClaimed,
                promo.PromotionCode,
                promo.PromotionClaimedAt,
                promo.PromotionAccessEndedAt
            FROM AspNetUsers u
            LEFT JOIN StudentProfiles pr ON pr.UserId = u.Id
            LEFT JOIN CurrentSubscription cs ON cs.UserId = u.Id
            CROSS JOIN FreePlan fp
            LEFT JOIN CurrentUsage usage ON usage.UserId = u.Id
            LEFT JOIN LastPayment lp ON lp.UserId = u.Id
            LEFT JOIN LaunchPromotion promo ON promo.UserId = u.Id
            WHERE u.Id = @UserId
            """, new { request.UserId });

        return row ?? throw new NotFoundException("Student not found.");
    }
}
