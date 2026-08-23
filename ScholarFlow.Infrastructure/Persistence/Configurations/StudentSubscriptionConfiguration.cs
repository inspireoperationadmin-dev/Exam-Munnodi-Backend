using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public sealed class StudentSubscriptionConfiguration : IEntityTypeConfiguration<StudentSubscription>
{
    public void Configure(EntityTypeBuilder<StudentSubscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.Notes)
            .HasMaxLength(1000);

        builder.Property(s => s.ActivationSource)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(ScholarFlow.Domain.Enums.SubscriptionActivationSource.AdminManual);

        builder.Property(s => s.PromotionCode)
            .HasMaxLength(80);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.EndsAt);
        builder.HasIndex(s => new { s.UserId, s.Status, s.StartsAt, s.EndsAt });
        builder.HasIndex(s => new { s.UserId, s.PromotionCode })
            .IsUnique()
            .HasFilter("[PromotionCode] IS NOT NULL");
    }
}
