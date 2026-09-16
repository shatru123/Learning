namespace LearningOS.Models;

public class UserSettings
{
    public int Id { get; set; }
    public int MaxExtraRecoveryMinutesPerDay { get; set; } = 60; // Configurable workload cap
    public int DailyTargetStudyMinutes { get; set; } = 120;
    public int WorkdayStartHour { get; set; } = 9; // 09:00
    public int WorkdayEndHour { get; set; } = 18; // 18:00
    public string PreferredStudyStartTime { get; set; } = "20:00";
    public string PreferredStudyEndTime { get; set; } = "22:30";
    public int WeekdayDailyAvailableMinutes { get; set; } = 120;
    public int WeekendDailyAvailableMinutes { get; set; } = 240;
    public bool ProtectWorkingHours { get; set; } = true;
    public DateOnly RoadmapStartDate { get; set; } = new(2026, 9, 21);
    public DateOnly RoadmapEndDate { get; set; } = new(2026, 12, 31);
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
