namespace LearningOS.Models;

public class Goal
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Career";
    public DateOnly? TargetDate { get; set; }
    public string Status { get; set; } = "Active"; // Active, Achieved, Postponed
    public int ProgressPercent { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
