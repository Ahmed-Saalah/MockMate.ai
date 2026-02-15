namespace MockMate.Api.Entities;

public class InterviewSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public string? Feedback { get; set; }
    public decimal Score { get; set; } = 0;
    public ICollection<SessionAnswer> Answers { get; set; } = new List<SessionAnswer>();
}
