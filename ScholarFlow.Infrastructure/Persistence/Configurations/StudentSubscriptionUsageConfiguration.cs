using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public sealed class StudentSubscriptionUsageConfiguration : IEntityTypeConfiguration<StudentSubscriptionUsage>
{
    public void Configure(EntityTypeBuilder<StudentSubscriptionUsage> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.PeriodStart)
            .HasColumnType("date");

        builder.Property(u => u.Feature)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(u => u.User)
            .WithMany()
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(u => new { u.UserId, u.PeriodStart, u.Feature })
            .IsUnique();
    }
}
