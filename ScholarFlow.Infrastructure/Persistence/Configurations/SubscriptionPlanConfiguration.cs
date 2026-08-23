using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(p => p.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Tier)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ScholarFlow.Domain.Enums.SubscriptionTier.Free);

        builder.Property(p => p.BillingCycle)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ScholarFlow.Domain.Enums.SubscriptionBillingCycle.Free);

        builder.Property(p => p.ProgressAccessLevel)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ScholarFlow.Domain.Enums.ProgressAccessLevel.Overall);

        builder.Property(p => p.BasePriceLkr).HasPrecision(10, 2);
        builder.Property(p => p.DiscountPercentage).HasPrecision(5, 2);
        builder.Property(p => p.PriceLkr).HasPrecision(10, 2);

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.SortOrder);
    }
}
