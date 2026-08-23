using Dapper;
using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Modules.Subscriptions.DTOs;

namespace ScholarFlow.Modules.Subscriptions.Queries.GetSubscriptionPlans;

public sealed class GetSubscriptionPlansQueryHandler(ISqlConnectionFactory sql)
    : IRequestHandler<GetSubscriptionPlansQuery, List<SubscriptionPlanDto>>
{
    public async Task<List<SubscriptionPlanDto>> Handle(GetSubscriptionPlansQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var rows = await conn.QueryAsync<SubscriptionPlanDto>("""
            SELECT
                Id,
                Code,
                Name,
                Tier,
                BillingCycle,
                DurationDays,
                BasePriceLkr,
                DiscountPercentage,
                PriceLkr,
                AllowsPaperExamMode,
                FreePastPaperCount,
                FreeModelPaperCount,
                MonthlyMockExamLimit,
                MonthlyUnitExamLimit,
                ProgressAccessLevel,
                CAST(CASE WHEN PriceLkr = 0 THEN 1 ELSE 0 END AS bit) AS IsFree,
                IsActive
            FROM SubscriptionPlans
            WHERE IsActive = 1
              AND IsDeleted = 0
            ORDER BY SortOrder
            """);

        return rows.ToList();
    }
}
