using LearningOS.Data;
using LearningOS.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningOS.Services;

public class AuditService : IAuditService
{
    private readonly LearningDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuditService> _logger;

    public AuditService(LearningDbContext db, ICurrentUserService currentUserService, ILogger<AuditService> logger)
    {
        _db = db;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public AuditService(LearningDbContext db, ILogger<AuditService> logger)
        : this(db, new CurrentUserService(new Microsoft.AspNetCore.Http.HttpContextAccessor()), logger)
    {
    }

    public async Task LogActivityAsync(string actionType, string entityName, string? entityId, string description, string? detailsJson = null)
    {
        try
        {
            var log = new ActivityAuditLog
            {
                UserId = _currentUserService.UserId ?? 1,
                ActionType = actionType,
                EntityName = entityName,
                EntityId = entityId,
                Description = description,
                DetailsJson = detailsJson,
                Timestamp = DateTime.UtcNow
            };
            _db.ActivityAuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for {ActionType} on {EntityName}", actionType, entityName);
        }
    }

    public async Task<List<ActivityAuditLog>> GetRecentActivityAsync(int limit = 30)
    {
        return await _db.ActivityAuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
}
