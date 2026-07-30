using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class StudentNotificationPreferenceConfiguration : IEntityTypeConfiguration<StudentNotificationPreference>
{
    public void Configure(EntityTypeBuilder<StudentNotificationPreference> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.TimeZoneId)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(p => p.DailyReminderTime)
            .HasColumnType("time")
            .IsRequired();

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.UserId).IsUnique();
    }
}
