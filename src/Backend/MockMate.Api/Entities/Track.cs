namespace MockMate.Api.Entities;

public class Track
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();
}
