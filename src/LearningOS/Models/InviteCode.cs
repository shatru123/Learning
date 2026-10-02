namespace LearningOS.Models;

public class InviteCode
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxUses { get; set; } = 1; // e.g. 1 for single-use, 50 for a batch
    public int UsedCount { get; set; } = 0;
    public DateTime? ExpiresAt { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
