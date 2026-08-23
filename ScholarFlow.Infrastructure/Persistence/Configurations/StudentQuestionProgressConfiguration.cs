using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class StudentQuestionProgressConfiguration : IEntityTypeConfiguration<StudentQuestionProgress>
{
    public void Configure(EntityTypeBuilder<StudentQuestionProgress> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.MasteryScore).HasPrecision(5, 2);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(p => p.LastAttemptMode)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne(p => p.Question)
            .WithMany()
            .HasForeignKey(p => p.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.SubTopic)
            .WithMany()
            .HasForeignKey(p => p.SubTopicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.UserId, p.QuestionId }).IsUnique();
        builder.HasIndex(p => new { p.UserId, p.SubjectId, p.TopicId });
        builder.HasIndex(p => new { p.UserId, p.SubTopicId, p.Status });
        builder.HasIndex(p => new { p.UserId, p.LastSeenAt });
        builder.HasIndex(p => new { p.UserId, p.LastAnswerCorrect });
    }
}
