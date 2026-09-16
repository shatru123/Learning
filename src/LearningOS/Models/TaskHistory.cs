namespace LearningOS.Models;

public class TaskHistory
{
    public int Id { get; set; }
    public int LearningTaskId { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
