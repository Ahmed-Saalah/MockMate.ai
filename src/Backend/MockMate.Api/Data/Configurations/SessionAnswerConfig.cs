using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MockMate.Api.Entities;

namespace MockMate.Api.Data.Configurations;

public class SessionAnswerConfig : IEntityTypeConfiguration<SessionAnswer>
{
    public void Configure(EntityTypeBuilder<SessionAnswer> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.InterviewSessionId, a.QuestionId }).IsUnique();

        builder
            .HasOne(a => a.Question)
            .WithMany()
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(a => a.SelectedOption)
            .WithMany()
            .HasForeignKey(a => a.SelectedOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Score stores a percentage (0.00–100.00).
        builder.Property(a => a.Score).HasPrecision(5, 2);

        builder.Property(a => a.Status).HasMaxLength(50);
    }
}
