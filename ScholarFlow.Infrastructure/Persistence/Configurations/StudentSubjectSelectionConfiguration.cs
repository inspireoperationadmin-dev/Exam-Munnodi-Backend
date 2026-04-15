using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class StudentSubjectSelectionConfiguration : IEntityTypeConfiguration<StudentSubjectSelection>
{
    public void Configure(EntityTypeBuilder<StudentSubjectSelection> builder)
    {
        builder.HasKey(ss => ss.Id);

        builder.HasOne(ss => ss.StudentProfile)
            .WithMany(sp => sp.SubjectSelections)
            .HasForeignKey(ss => ss.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ss => ss.Subject)
            .WithMany()
            .HasForeignKey(ss => ss.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ss => new { ss.StudentProfileId, ss.SubjectId }).IsUnique();
    }
}
