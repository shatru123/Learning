namespace LearningOS.Models;

public class SystemDesignTopic
{
    public int Id { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public string? ArchitectureSummary { get; set; }
    public string? KeyComponents { get; set; }
    public string? Notes { get; set; }
    public string? DiagramUrl { get; set; }
    public string Status { get; set; } = "Planned"; // Planned, InProgress, Studied
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
