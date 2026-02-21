namespace MockMate.Api.Entities;

public class Question
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string SeniorityLevel { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();
    public int? Judge0LanguageId { get; set; }
    public decimal? TimeLimit { get; set; }
    public int? MemoryLimit { get; set; }
    public string? DefaultCode { get; set; }
    public string? DriverCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<McqOption> Options { get; set; } = new List<McqOption>();
    public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
}
