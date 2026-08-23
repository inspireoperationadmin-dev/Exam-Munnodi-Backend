using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Interfaces.Repositories;

public interface ISubscriptionRepository
{
    Task<SubscriptionPlan?> GetPlanByCodeAsync(SubscriptionPlanCode code, CancellationToken ct = default);
    Task<StudentSubscription?> GetCurrentActiveAsync(Guid userId, CancellationToken ct = default);
    Task<StudentSubscription?> GetNextScheduledAsync(Guid userId, CancellationToken ct = default);
    Task<StudentSubscription?> GetSubscriptionAsync(Guid subscriptionId, CancellationToken ct = default);
    Task<DateTime?> GetActivePlanAccessEndAsync(Guid userId, Guid planId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentSubscription>> GetActiveSubscriptionsAsync(Guid userId, CancellationToken ct = default);
    Task<bool> HasPromotionAsync(Guid userId, string promotionCode, CancellationToken ct = default);
    Task<bool> TryAddPromotionSubscriptionAsync(StudentSubscription subscription, CancellationToken ct = default);
    Task<int> GetUsageAsync(Guid userId, DateTime periodStart, SubscriptionUsageFeature feature, CancellationToken ct = default);
    Task<bool> TryConsumeUsageAsync(Guid userId, DateTime periodStart, SubscriptionUsageFeature feature, int limit, CancellationToken ct = default);
    Task ReleaseUsageAsync(Guid userId, DateTime periodStart, SubscriptionUsageFeature feature, CancellationToken ct = default);
    Task AddSubscriptionAsync(StudentSubscription subscription, CancellationToken ct = default);
    Task AddPaymentAsync(SubscriptionPayment payment, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
