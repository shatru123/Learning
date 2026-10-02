namespace LearningOS.Services;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? UserRole { get; }
    string? UserEmail { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}
