namespace LearningOS.Models;

public class LeaveDayRecord
{
    public int Id { get; set; }
    public int? DayPlanId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string LeaveType { get; set; } = "Personal work"; // Sick leave, Vacation, Wedding/family event, Travel, Personal work
    public string? Reason { get; set; }
    public bool PreservesStreak { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
