using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class PaperConfiguration : IEntityTypeConfiguration<Paper>
{
    public void Configure(EntityTypeBuilder<Paper> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Title).IsRequired().HasMaxLength(300);
        builder.Property(p => p.Year).IsRequired();
        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.Medium).HasConversion<string>().HasMaxLength(15);
        builder.Property(p => p.NegativeMarkValue).HasPrecision(4, 2);
        builder.Property(p => p.Sitting).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.OfficialPaperCode).HasMaxLength(30);

        builder.HasOne(p => p.Subject)
            .WithMany(s => s.Papers)
            .HasForeignKey(p => p.SubjectId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.SubjectId, p.Year, p.Type, p.Medium });
        builder.HasIndex(p => p.IsPublic);
        builder.HasIndex(p => p.CreatedByTeacherId);
    }
}
