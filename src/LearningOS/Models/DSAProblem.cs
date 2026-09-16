namespace LearningOS.Models;

public class DSAProblem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Medium"; // Easy, Medium, Hard
    public string Platform { get; set; } = "LeetCode"; // LeetCode, NeetCode, HackerRank, Other
    public string? Pattern { get; set; } // Two Pointers, Sliding Window, DP, Graph, Tree, etc.
    public string? ProblemUrl { get; set; }
    public string? SolutionNotes { get; set; }
    public string Status { get; set; } = "Planned"; // Solved, InProgress, Planned, ReviewNeeded
    public DateTime? SolvedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
