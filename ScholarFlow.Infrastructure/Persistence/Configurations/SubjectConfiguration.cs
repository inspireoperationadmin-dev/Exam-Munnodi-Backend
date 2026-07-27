using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.NameEnglish).IsRequired().HasMaxLength(150);
        builder.Property(s => s.NameTamil).HasMaxLength(150);
        builder.Property(s => s.NameSinhala).HasMaxLength(150);
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.HasIndex(s => s.NameEnglish).IsUnique();
    }
}
