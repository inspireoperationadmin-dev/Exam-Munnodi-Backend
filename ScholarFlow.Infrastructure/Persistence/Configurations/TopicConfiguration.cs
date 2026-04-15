using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TopicName).IsRequired().HasMaxLength(200);
        builder.Property(t => t.OrderIndex).HasDefaultValue(0);

        builder.HasOne(t => t.Subject)
            .WithMany(s => s.Topics)
            .HasForeignKey(t => t.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.SubjectId, t.OrderIndex });
    }
}
