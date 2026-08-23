using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class ExamSessionConfiguration : IEntityTypeConfiguration<ExamSession>
{
    public void Configure(EntityTypeBuilder<ExamSession> builder)
    {
        builder.HasKey(es => es.Id);

        builder.Property(es => es.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(es => es.Mode).HasConversion<string>().HasMaxLength(20);
        builder.Property(es => es.LastActivityAt).IsRequired();
        builder.Property(es => es.TimeLimitMinutes).IsRequired(false);
        builder.Property(es => es.ExpiresAt).IsRequired(false);
        builder.Property(es => es.FinalScore).HasPrecision(5, 2);
        builder.Property(es => es.ObtainedMarks).HasPrecision(8, 2);
        builder.Property(es => es.TotalMarks).HasPrecision(8, 2);

        builder.Ignore(es => es.Duration);

        builder.HasOne(es => es.User)
            .WithMany(u => u.ExamSessions)
            .HasForeignKey(es => es.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(es => es.Paper)
            .WithMany(p => p.ExamSessions)
            .HasForeignKey(es => es.PaperId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(es => es.Subject)
            .WithMany()
            .HasForeignKey(es => es.SubjectId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(es => es.Topic)
            .WithMany()
            .HasForeignKey(es => es.TopicId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Backing fields for private collections
        builder.Navigation(es => es.UserResponses).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(es => es.SessionQuestions).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(es => es.UserId);
        builder.HasIndex(es => es.Mode);
        builder.HasIndex(es => es.ExpiresAt);
        builder.HasIndex(es => new { es.EndTime, es.Status });
        builder.HasIndex(es => es.LastActivityAt);
        builder.HasIndex(es => new { es.UserId, es.Status });
        builder.HasIndex(es => new { es.UserId, es.PaperId, es.Mode, es.Status });
        builder.HasIndex(es => new { es.UserId, es.TopicId, es.Mode, es.Status });
        builder.HasIndex(es => new { es.PaperId, es.FinalScore, es.Status });
    }
}
