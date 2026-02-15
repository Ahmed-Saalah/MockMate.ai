using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MockMate.Api.Entities;

namespace MockMate.Api.Data.Configurations;

public class InterviewSessionConfig : IEntityTypeConfiguration<InterviewSession>
{
    public void Configure(EntityTypeBuilder<InterviewSession> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Score).HasPrecision(5, 2);

        builder
            .HasMany(s => s.Answers)
            .WithOne(a => a.InterviewSession)
            .HasForeignKey(a => a.InterviewSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
