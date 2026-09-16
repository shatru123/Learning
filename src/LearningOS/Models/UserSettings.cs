namespace LearningOS.Models;

public class UserSettings
{
    public int Id { get; set; }
    public int MaxExtraRecoveryMinutesPerDay { get; set; } = 60; // Configurable workload cap
    public int DailyTargetStudyMinutes { get; set; } = 120;
    public DateOnly RoadmapStartDate { get; set; } = new(2026, 9, 21);
    public DateOnly RoadmapEndDate { get; set; } = new(2026, 12, 31);
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
