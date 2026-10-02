namespace LearningOS.Models;

public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Learner"; // "Admin", "Learner"
    public string Status { get; set; } = "Active"; // "Active", "PendingApproval", "Rejected"
    public DateOnly RequestedStartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string? InviteCodeUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
}
