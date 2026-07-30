using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class NotificationDeviceConfiguration : IEntityTypeConfiguration<NotificationDevice>
{
    public void Configure(EntityTypeBuilder<NotificationDevice> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Platform)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.Provider)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.Endpoint).HasMaxLength(2048);
        builder.Property(d => d.EndpointHash).HasMaxLength(64);
        builder.Property(d => d.P256dh).HasMaxLength(512);
        builder.Property(d => d.Auth).HasMaxLength(512);
        builder.Property(d => d.PushToken).HasMaxLength(4000);
        builder.Property(d => d.PushTokenHash).HasMaxLength(64);
        builder.Property(d => d.DeviceName).HasMaxLength(120);
        builder.Property(d => d.UserAgent).HasMaxLength(512);
        builder.Property(d => d.AppVersion).HasMaxLength(40);

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.UserId, d.Platform, d.Provider, d.IsActive });
        builder.HasIndex(d => d.EndpointHash)
            .IsUnique()
            .HasFilter("[EndpointHash] IS NOT NULL");
        builder.HasIndex(d => d.PushTokenHash)
            .IsUnique()
            .HasFilter("[PushTokenHash] IS NOT NULL");
    }
}
