namespace LearningOS.Models;

public class StudySession
{
    public int Id { get; set; }
    public int? DayPlanId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public DateOnly SessionDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
