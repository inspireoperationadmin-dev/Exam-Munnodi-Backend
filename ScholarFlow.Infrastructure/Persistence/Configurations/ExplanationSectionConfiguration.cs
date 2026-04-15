using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class ExplanationSectionConfiguration : IEntityTypeConfiguration<ExplanationSection>
{
    public void Configure(EntityTypeBuilder<ExplanationSection> builder)
    {
        builder.HasKey(es => es.Id);
        builder.Property(es => es.Title).IsRequired().HasMaxLength(200);
        builder.Property(es => es.Content).IsRequired();

        builder.HasOne(es => es.Explanation)
            .WithMany(e => e.Sections)
            .HasForeignKey(es => es.ExplanationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(es => new { es.ExplanationId, es.OrderIndex });
    }
}
