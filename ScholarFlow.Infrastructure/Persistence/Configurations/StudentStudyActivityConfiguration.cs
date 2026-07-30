using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class StudentStudyActivityConfiguration : IEntityTypeConfiguration<StudentStudyActivity>
{
    public void Configure(EntityTypeBuilder<StudentStudyActivity> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ActivityDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(a => a.Mode)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Session)
            .WithMany()
            .HasForeignKey(a => a.SessionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Subject)
            .WithMany()
            .HasForeignKey(a => a.SubjectId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.UserId, a.ActivityDate });
        builder.HasIndex(a => a.SessionId)
            .IsUnique()
            .HasFilter("[SessionId] IS NOT NULL");
    }
}
