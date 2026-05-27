using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Infrastructure.Persistence.Configurations;

public class UserResponseConfiguration : IEntityTypeConfiguration<UserResponse>
{
    public void Configure(EntityTypeBuilder<UserResponse> builder)
    {
        builder.HasKey(ur => ur.Id);

        // Map the new OrderIndex property as required with a default value of 0 [1]
        builder.Property(ur => ur.OrderIndex)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(ur => ur.ResponseStatus).HasConversion<string>().HasMaxLength(15);
        builder.Property(ur => ur.MarksAwarded).HasPrecision(6, 2);

        builder.HasOne(ur => ur.Session)
            .WithMany(es => es.UserResponses)
            .HasForeignKey(ur => ur.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Question)
            .WithMany(q => q.UserResponses)
            .HasForeignKey(ur => ur.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ur => ur.SelectedOption)
            .WithMany()
            .HasForeignKey(ur => ur.SelectedOptionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ur => ur.SessionId);
        builder.HasIndex(ur => new { ur.SessionId, ur.QuestionId }).IsUnique();
    }
}