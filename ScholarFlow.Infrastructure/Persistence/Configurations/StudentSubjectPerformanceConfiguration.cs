using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class StudentSubjectPerformanceConfiguration : IEntityTypeConfiguration<StudentSubjectPerformance>
{
    public void Configure(EntityTypeBuilder<StudentSubjectPerformance> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.AverageExamScore).HasPrecision(5, 2);
        builder.Property(p => p.BestScore).HasPrecision(5, 2);
        builder.Property(p => p.OverallCorrectPercentage).HasPrecision(5, 2);

        builder.HasOne(p => p.Subject)
            .WithMany()
            .HasForeignKey(p => p.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.UserId, p.SubjectId }).IsUnique();
    }
}
