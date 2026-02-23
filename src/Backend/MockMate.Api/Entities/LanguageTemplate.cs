namespace MockMate.Api.Entities;

public class LanguageTemplate
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public int LanguageId { get; set; }
    public decimal TimeLimit { get; set; }
    public int MemoryLimit { get; set; }
    public string? DefaultCode { get; set; }
    public string? DriverCode { get; set; }
}
