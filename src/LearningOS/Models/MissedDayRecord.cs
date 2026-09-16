namespace LearningOS.Models;

public class MissedDayRecord
{
    public int Id { get; set; }
    public int DayPlanId { get; set; }
    public DateOnly MissedDate { get; set; }
    public string Reason { get; set; } = "Other"; // Work, Health, Family, Travel, Personal, Too much workload, Lack of time, Other
    public string? Note { get; set; }
    public string? RecoveryStrategy { get; set; } // MoveToTomorrow, Reschedule, Compress, Skip, ExtendRoadmap
    public DateTime? RecoveredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
