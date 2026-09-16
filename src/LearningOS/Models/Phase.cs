namespace LearningOS.Models;

public class Phase
{
    public int Id { get; set; }
    public int LearningPlanId { get; set; }
    public int PhaseNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int StartDay { get; set; }
    public int EndDay { get; set; }
}
