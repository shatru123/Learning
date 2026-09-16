namespace LearningOS.Models;

public class JobApplication
{
    public int Id { get; set; }
    public string Company { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = "Wishlist"; // Wishlist, Applied, Screening, Technical, Final, Offer, Rejected
    public DateOnly? AppliedDate { get; set; }
    public string? JobUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
