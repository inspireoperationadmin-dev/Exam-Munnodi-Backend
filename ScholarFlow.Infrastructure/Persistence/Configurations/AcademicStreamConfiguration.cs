using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class AcademicStreamConfiguration : IEntityTypeConfiguration<AcademicStream>
{
    public void Configure(EntityTypeBuilder<AcademicStream> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.HasIndex(s => s.Name).IsUnique();
    }
}
