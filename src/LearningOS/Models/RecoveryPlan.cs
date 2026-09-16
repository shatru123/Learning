namespace LearningOS.Models;

public class RecoveryPlan
{
    public int Id { get; set; }
    public int DayPlanId { get; set; }
    public string Strategy { get; set; } = string.Empty; // MoveToTomorrow, Reschedule, Compress, Skip, ExtendRoadmap
    public int ExtraMinutesPerDay { get; set; } = 60;
    public string? DetailsJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
