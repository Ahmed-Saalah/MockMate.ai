using System.Text.Json.Serialization;

namespace MockMate.Api.Clients.AiService.Dtos;

public class GenerateInterviewResponse
{
    public string TrackName { get; set; } = string.Empty;
    public string SeniorityLevel { get; set; } = string.Empty;
    public List<string> DetectedSkills { get; set; } = new();
    public List<AiMcqQuestion> McqQuestions { get; set; } = new();
    public List<AiCodingQuestion> CodingQuestions { get; set; } = new();
}

public class AiMcqQuestion
{
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<AiMcqOption> Options { get; set; } = new();
}

public class AiMcqOption
{
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class AiCodingQuestion
{
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<AiTestCase> TestCases { get; set; } = new();
    public List<AiCodeTemplate> Templates { get; set; } = new();
}

public class AiTestCase
{
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
}

public class AiCodeTemplate
{
    public int LanguageId { get; set; }
    public string DefaultCode { get; set; } = string.Empty;
    public string DriverCode { get; set; } = string.Empty;
}
