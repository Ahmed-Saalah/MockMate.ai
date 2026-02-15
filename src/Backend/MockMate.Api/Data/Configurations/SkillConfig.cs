using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MockMate.Api.Entities;

namespace MockMate.Api.Data.Configurations;

public class SkillConfig : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);

        builder
            .HasMany(s => s.Questions)
            .WithOne(q => q.Skill)
            .HasForeignKey(q => q.SkillId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
