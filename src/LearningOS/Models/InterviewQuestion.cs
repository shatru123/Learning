namespace LearningOS.Models;

public class InterviewQuestion
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Category { get; set; } = ".NET/C#"; // .NET/C#, Architecture, Distributed Systems, SQL/DB, AI/LLM, Behavioral
    public string? AnswerNotes { get; set; }
    public string ConfidenceLevel { get; set; } = "Medium"; // Low, Medium, High
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
