namespace MockMate.Api.Entities;

public class Question
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string SeniorityLevel { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();
    public ICollection<McqOption> Options { get; set; } = new List<McqOption>();
    public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
    public ICollection<LanguageTemplate> Templates { get; set; } = new List<LanguageTemplate>();
}
