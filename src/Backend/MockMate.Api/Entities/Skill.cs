namespace MockMate.Api.Entities;

public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Track> Tracks { get; set; } = new List<Track>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
