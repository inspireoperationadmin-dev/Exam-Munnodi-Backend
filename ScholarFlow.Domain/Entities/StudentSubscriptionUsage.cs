using ScholarFlow.Domain.Entities.Base;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Entities;

public sealed class StudentSubscriptionUsage : AuditableEntity
{
    public Guid UserId { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public SubscriptionUsageFeature Feature { get; private set; }
    public int UsedCount { get; private set; }

    public ApplicationUser User { get; set; } = null!;

    private StudentSubscriptionUsage() { }

    public static StudentSubscriptionUsage Create(
        Guid userId,
        DateTime periodStart,
        SubscriptionUsageFeature feature)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PeriodStart = periodStart.Date,
            Feature = feature,
            UsedCount = 1
        };

    public void Increment() => UsedCount++;
}
