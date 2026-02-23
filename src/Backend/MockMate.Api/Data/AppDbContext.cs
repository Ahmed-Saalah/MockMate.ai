using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Data.Seeding;
using MockMate.Api.Entities;

namespace MockMate.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, IdentityRole<int>, int>(options)
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Track> Tracks { get; set; }
    public DbSet<Skill> Skills { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<LanguageTemplate> LanguageTemplates { get; set; }
    public DbSet<TestCase> TestCases { get; set; }
    public DbSet<McqOption> McqOptions { get; set; }
    public DbSet<InterviewSession> InterviewSessions { get; set; }
    public DbSet<SessionAnswer> SessionAnswers { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        builder.SeedRolesAndAdminAsync();
    }
}
