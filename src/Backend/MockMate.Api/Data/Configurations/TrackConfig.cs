using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MockMate.Api.Entities;

namespace MockMate.Api.Data.Configurations;

public class TrackConfig : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);

        builder
            .HasMany(t => t.Skills)
            .WithMany(s => s.Tracks)
            .UsingEntity(j => j.ToTable("TrackSkills"));
    }
}
