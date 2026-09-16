namespace LearningOS.Models;

public class LearningPlan
{
    public int Id { get; set; }
    public string Title { get; set; } = "100-Day Software Engineering Mastery";
    public int TotalLearningDays { get; set; } = 100;
    public DateOnly StartDate { get; set; } = new(2026, 9, 21);
    public DateOnly EndDate { get; set; } = new(2026, 12, 31);
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<Phase> Phases { get; set; } = new();
}
