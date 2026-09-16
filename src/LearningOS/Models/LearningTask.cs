namespace LearningOS.Models;

public class LearningTask
{
    public int Id { get; set; }
    public int DayPlanId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "Core"; // DSA, SystemDesign, AILearning, DotNetBackend, CloudDevOps, InterviewPrep, PortfolioProject, Core
    public int EstimatedMinutes { get; set; } = 45;
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Skipped, Rescheduled
    public int OriginalDayNumber { get; set; }
    public int CurrentDayNumber { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<TaskHistory> History { get; set; } = new();
}
