using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class StudentSubTopicPerformanceConfiguration : IEntityTypeConfiguration<StudentSubTopicPerformance>
{
    public void Configure(EntityTypeBuilder<StudentSubTopicPerformance> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.CorrectPercentage).HasPrecision(5, 2);

        builder.HasOne(p => p.SubTopic)
            .WithMany()
            .HasForeignKey(p => p.SubTopicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.UserId, p.SubTopicId }).IsUnique();
        builder.HasIndex(p => new { p.UserId, p.SubjectId, p.CorrectPercentage });
    }
}
