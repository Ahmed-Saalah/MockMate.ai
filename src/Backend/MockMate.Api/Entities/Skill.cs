namespace MockMate.Api.Entities;

public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int TrackId { get; set; }
    public Track Track { get; set; } = null!;

    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
