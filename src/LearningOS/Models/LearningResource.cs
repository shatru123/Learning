namespace LearningOS.Models;

public class LearningResource
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Category { get; set; } = "Documentation"; // Documentation, Article, Video, Book, Repository
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
