namespace LearningOS.Models;

public class ActivityAuditLog
{
    public int Id { get; set; }
    public string ActionType { get; set; } = string.Empty; // TaskCompleted, TaskUpdated, DayMissed, DayRecovered, RestDayAdded, LeaveDayAdded, ReviewCompleted, RoadmapExtended, ResourceAdded, NoteCreated
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
