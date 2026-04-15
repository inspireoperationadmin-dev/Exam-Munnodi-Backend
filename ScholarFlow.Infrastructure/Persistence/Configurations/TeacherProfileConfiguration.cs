using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class TeacherProfileConfiguration : IEntityTypeConfiguration<TeacherProfile>
{
    public void Configure(EntityTypeBuilder<TeacherProfile> builder)
    {
        builder.HasKey(tp => tp.Id);
        builder.Property(tp => tp.FullName).IsRequired().HasMaxLength(150);
        builder.Property(tp => tp.PhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(tp => tp.Qualification).IsRequired().HasMaxLength(300);
        builder.Property(tp => tp.Bio).HasMaxLength(1000);
        builder.Property(tp => tp.TeacherCode).IsRequired().HasMaxLength(20);
        builder.Property(tp => tp.Status).HasConversion<string>().HasMaxLength(15);
        builder.Property(tp => tp.RejectionReason).HasMaxLength(500);

        builder.HasOne(tp => tp.User)
            .WithOne()
            .HasForeignKey<TeacherProfile>(tp => tp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tp => tp.Subject)
            .WithMany()
            .HasForeignKey(tp => tp.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(tp => tp.UserId).IsUnique();
        builder.HasIndex(tp => tp.TeacherCode).IsUnique();
        builder.HasIndex(tp => tp.Status);
    }
}
