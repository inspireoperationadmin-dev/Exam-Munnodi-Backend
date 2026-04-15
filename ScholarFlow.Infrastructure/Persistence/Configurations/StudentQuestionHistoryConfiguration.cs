using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class StudentQuestionHistoryConfiguration : IEntityTypeConfiguration<StudentQuestionHistory>
{
    public void Configure(EntityTypeBuilder<StudentQuestionHistory> builder)
    {
        builder.HasKey(h => h.Id);

        builder.HasOne(h => h.Question)
            .WithMany()
            .HasForeignKey(h => h.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => new { h.UserId, h.QuestionId }).IsUnique();
        builder.HasIndex(h => new { h.UserId, h.LastAnswerCorrect });
    }
}
