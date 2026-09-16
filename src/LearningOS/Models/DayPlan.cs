namespace LearningOS.Models;

public class DayPlan
{
    public int Id { get; set; }
    public int? DayNumber { get; set; } // 1..100 for core learning days; null for dedicated rest/leave calendar entries
    public bool IsLearningDay { get; set; } = true;
    public DateOnly CalendarDate { get; set; }
    public DayStatus Status { get; set; } = DayStatus.Planned;
    public string Title { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? ReviewSummary { get; set; }
    public int PhaseId { get; set; }
    public int WeekNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<LearningTask> Tasks { get; set; } = new();
    public List<DayStatusHistory> StatusHistory { get; set; } = new();
    public MissedDayRecord? MissedRecord { get; set; }
    public RestDayRecord? RestRecord { get; set; }
    public LeaveDayRecord? LeaveRecord { get; set; }
    public DailyReview? DailyReview { get; set; }
}
