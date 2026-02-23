using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MockMate.Api.Entities;

namespace MockMate.Api.Data.Configurations;

public class LanguageTemplateConfig : IEntityTypeConfiguration<LanguageTemplate>
{
    public void Configure(EntityTypeBuilder<LanguageTemplate> builder)
    {
        builder.HasKey(qt => qt.Id);

        builder.HasIndex(qt => new { qt.QuestionId, qt.LanguageId }).IsUnique();

        builder.Property(qt => qt.LanguageId).IsRequired();

        builder.Property(qt => qt.TimeLimit).HasPrecision(5, 2).IsRequired();
        builder.Property(qt => qt.MemoryLimit).IsRequired();

        builder.Property(qt => qt.DefaultCode).IsRequired(false);
        builder.Property(qt => qt.DriverCode).IsRequired(false);

        builder
            .HasOne(qt => qt.Question)
            .WithMany(q => q.Templates)
            .HasForeignKey(qt => qt.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
