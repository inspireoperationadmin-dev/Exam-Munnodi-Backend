using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class ExamSessionQuestionConfiguration : IEntityTypeConfiguration<ExamSessionQuestion>
{
    public void Configure(EntityTypeBuilder<ExamSessionQuestion> builder)
    {
        builder.HasKey(esq => esq.Id);

        builder.HasOne(esq => esq.Session)
            .WithMany(es => es.SessionQuestions)
            .HasForeignKey(esq => esq.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(esq => esq.Question)
            .WithMany()
            .HasForeignKey(esq => esq.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(esq => new { esq.SessionId, esq.QuestionId }).IsUnique();
        builder.HasIndex(esq => new { esq.SessionId, esq.OrderIndex });
    }
}
