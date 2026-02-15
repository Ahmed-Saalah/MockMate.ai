namespace MockMate.Api.Entities;

public class Question
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string SeniorityLevel { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
    public int? Judge0LanguageId { get; set; }
    public decimal? TimeLimit { get; set; }
    public int? MemoryLimit { get; set; }
    public string? DefaultCode { get; set; }
    public string? DriverCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<McqOption> Options { get; set; } = new List<McqOption>();
    public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
}
