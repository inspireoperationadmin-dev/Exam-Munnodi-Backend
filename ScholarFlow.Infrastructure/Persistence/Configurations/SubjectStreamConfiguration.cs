using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class SubjectStreamConfiguration : IEntityTypeConfiguration<SubjectStream>
{
    public void Configure(EntityTypeBuilder<SubjectStream> builder)
    {
        builder.HasKey(ss => new { ss.SubjectId, ss.StreamId });

        builder.HasOne(ss => ss.Subject)
            .WithMany(s => s.SubjectStreams)
            .HasForeignKey(ss => ss.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ss => ss.Stream)
            .WithMany(s => s.SubjectStreams)
            .HasForeignKey(ss => ss.StreamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
