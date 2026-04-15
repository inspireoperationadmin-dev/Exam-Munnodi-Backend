using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class StudentTeacherConnectionConfiguration : IEntityTypeConfiguration<StudentTeacherConnection>
{
    public void Configure(EntityTypeBuilder<StudentTeacherConnection> builder)
    {
        builder.HasKey(stc => stc.Id);
        builder.Property(stc => stc.Status).HasConversion<string>().HasMaxLength(15);

        builder.HasOne(stc => stc.StudentProfile)
            .WithMany(sp => sp.TeacherConnections)
            .HasForeignKey(stc => stc.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(stc => stc.TeacherProfile)
            .WithMany(tp => tp.StudentConnections)
            .HasForeignKey(stc => stc.TeacherProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(stc => new { stc.StudentProfileId, stc.TeacherProfileId }).IsUnique();
        builder.HasIndex(stc => stc.Status);
    }
}
