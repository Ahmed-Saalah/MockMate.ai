namespace MockMate.Api.Entities;

public class SessionAnswer
{
    public int Id { get; set; }
    public int InterviewSessionId { get; set; }
    public InterviewSession InterviewSession { get; set; } = null!;
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public int? SelectedOptionId { get; set; }
    public McqOption? SelectedOption { get; set; }
    public string? SubmittedCode { get; set; }
    public int? LanguageId { get; set; }
    public decimal Score { get; set; } = 0;
    public string? Status { get; set; }
    public bool IsCorrect { get; set; }
}
