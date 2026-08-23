using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPlanPriceChangeConfiguration
    : IEntityTypeConfiguration<SubscriptionPlanPriceChange>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlanPriceChange> builder)
    {
        builder.HasKey(change => change.Id);

        builder.Property(change => change.PreviousBasePriceLkr).HasPrecision(10, 2);
        builder.Property(change => change.PreviousDiscountPercentage).HasPrecision(5, 2);
        builder.Property(change => change.PreviousPriceLkr).HasPrecision(10, 2);
        builder.Property(change => change.NewBasePriceLkr).HasPrecision(10, 2);
        builder.Property(change => change.NewDiscountPercentage).HasPrecision(5, 2);
        builder.Property(change => change.NewPriceLkr).HasPrecision(10, 2);
        builder.Property(change => change.Note).HasMaxLength(500);

        builder.HasOne(change => change.Plan)
            .WithMany()
            .HasForeignKey(change => change.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(change => change.PlanId);
        builder.HasIndex(change => change.CreatedAt);
    }
}
