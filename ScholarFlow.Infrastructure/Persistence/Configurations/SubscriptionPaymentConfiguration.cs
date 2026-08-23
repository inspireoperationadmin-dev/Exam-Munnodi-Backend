using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPaymentConfiguration : IEntityTypeConfiguration<SubscriptionPayment>
{
    public void Configure(EntityTypeBuilder<SubscriptionPayment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.AmountLkr)
            .HasPrecision(10, 2);

        builder.Property(p => p.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.ReferenceNumber)
            .HasMaxLength(120);

        builder.Property(p => p.ReceiptImageUrl)
            .HasMaxLength(1000);

        builder.Property(p => p.AdminNote)
            .HasMaxLength(1000);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Subscription)
            .WithMany()
            .HasForeignKey(p => p.SubscriptionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.SubscriptionId);
        builder.HasIndex(p => p.ReviewedAt);
    }
}
