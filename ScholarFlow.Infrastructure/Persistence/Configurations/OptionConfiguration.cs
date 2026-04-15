using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class OptionConfiguration : IEntityTypeConfiguration<Option>
{
    public void Configure(EntityTypeBuilder<Option> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Label).IsRequired().HasMaxLength(5);
        builder.Property(o => o.OptionText).IsRequired().HasMaxLength(1000);
        builder.Property(o => o.OptionImageUrl).HasMaxLength(1000);

        builder.HasOne(o => o.Question)
            .WithMany(q => q.Options)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.QuestionId);
    }
}
