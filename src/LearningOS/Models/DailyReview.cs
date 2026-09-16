namespace LearningOS.Models;

public class DailyReview
{
    public int Id { get; set; }
    public int DayPlanId { get; set; }
    public DateOnly ReviewDate { get; set; }
    public string Rating { get; set; } = "Completed everything"; // Completed everything, Completed most, Completed some, Missed, Rest day
    public string? WhatHappenedNotes { get; set; }
    public string? CarryForwardNotes { get; set; }
    public string? TomorrowPriority { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
