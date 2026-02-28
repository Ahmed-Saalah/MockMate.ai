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
        builder.Property(s => s.NormalizedName).IsRequired().HasMaxLength(100);
        builder.HasIndex(s => s.NormalizedName).IsUnique();

        builder
            .HasMany(s => s.Questions)
            .WithMany(q => q.Skills)
            .UsingEntity(j => j.ToTable("QuestionSkills"));
    }
}
