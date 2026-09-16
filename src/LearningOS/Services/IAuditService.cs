using LearningOS.Models;

namespace LearningOS.Services;

public interface IAuditService
{
    Task LogActivityAsync(string actionType, string entityName, string? entityId, string description, string? detailsJson = null);
    Task<List<ActivityAuditLog>> GetRecentActivityAsync(int limit = 30);
}
