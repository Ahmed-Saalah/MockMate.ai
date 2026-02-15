using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MockMate.Api.Entities;

namespace MockMate.Api.Data.Configurations;

public class TestCaseConfig : IEntityTypeConfiguration<TestCase>
{
    public void Configure(EntityTypeBuilder<TestCase> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Input).IsRequired();
        builder.Property(t => t.Output).IsRequired();
    }
}
