using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MockMate.Api.Entities;

namespace MockMate.Api.Data.Configurations;

public class McqOptionConfig : IEntityTypeConfiguration<McqOption>
{
    public void Configure(EntityTypeBuilder<McqOption> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OptionText).IsRequired();
    }
}
