using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.FullName).IsRequired().HasMaxLength(150);
        builder.Property(sp => sp.PhoneNumber).HasMaxLength(20);

        builder.HasOne(sp => sp.User)
            .WithOne()
            .HasForeignKey<StudentProfile>(sp => sp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sp => sp.AcademicStream)
            .WithMany()
            .HasForeignKey(sp => sp.AcademicStreamId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(sp => sp.UserId).IsUnique();
    }
}
