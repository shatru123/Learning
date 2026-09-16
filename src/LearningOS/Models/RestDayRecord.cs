namespace LearningOS.Models;

public class RestDayRecord
{
    public int Id { get; set; }
    public int? DayPlanId { get; set; }
    public DateOnly Date { get; set; }
    public string? Reason { get; set; }
    public bool IsPrePlanned { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
