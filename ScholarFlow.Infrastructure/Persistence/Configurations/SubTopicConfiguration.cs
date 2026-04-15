using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class SubTopicConfiguration : IEntityTypeConfiguration<SubTopic>
{
    public void Configure(EntityTypeBuilder<SubTopic> builder)
    {
        builder.HasKey(st => st.Id);
        builder.Property(st => st.SubTopicName).IsRequired().HasMaxLength(200);
        builder.Property(st => st.OrderIndex).HasDefaultValue(0);

        builder.HasOne(st => st.Topic)
            .WithMany(t => t.SubTopics)
            .HasForeignKey(st => st.TopicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(st => new { st.TopicId, st.OrderIndex });
    }
}
