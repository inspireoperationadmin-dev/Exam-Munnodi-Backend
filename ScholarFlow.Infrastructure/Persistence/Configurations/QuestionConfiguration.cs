using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.QuestionText).IsRequired().HasMaxLength(2000);
        builder.Property(q => q.QuestionImageUrl).HasMaxLength(1000);
        builder.Property(q => q.Marks)
            .IsRequired()
            .HasPrecision(5, 2)
            .HasDefaultValue(2m);                         // ← added

        builder.HasOne(q => q.Paper)
            .WithMany(p => p.Questions)
            .HasForeignKey(q => q.PaperId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.SubTopic)
            .WithMany(st => st.Questions)
            .HasForeignKey(q => q.SubTopicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(q => q.PaperId);
        builder.HasIndex(q => q.SubTopicId);
        builder.HasIndex(q => new { q.PaperId, q.OrderIndex });
    }
}