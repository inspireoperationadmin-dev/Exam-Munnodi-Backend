using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class ExplanationConfiguration : IEntityTypeConfiguration<Explanation>
{
    public void Configure(EntityTypeBuilder<Explanation> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(10);
        builder.Property(e => e.VideoUrl).HasMaxLength(500);

        builder.HasOne(e => e.Question)
            .WithOne(q => q.Explanation)
            .HasForeignKey<Explanation>(e => e.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
