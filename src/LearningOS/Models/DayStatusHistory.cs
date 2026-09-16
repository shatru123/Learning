namespace LearningOS.Models;

public class DayStatusHistory
{
    public int Id { get; set; }
    public int DayPlanId { get; set; }
    public DayStatus OldStatus { get; set; }
    public DayStatus NewStatus { get; set; }
    public string? Reason { get; set; }
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
