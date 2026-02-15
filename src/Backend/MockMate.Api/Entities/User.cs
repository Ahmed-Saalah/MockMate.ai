using Microsoft.AspNetCore.Identity;

namespace MockMate.Api.Entities;

public sealed class User : IdentityUser<int>
{
    public string? DisplayName { get; set; }
    public string? AvatarPath { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? LoggedInAt { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<InterviewSession> InterviewSessions { get; set; } =
        new List<InterviewSession>();
}
