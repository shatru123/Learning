using LearningOS.Data;
using LearningOS.Dtos;
using LearningOS.Models;
using LearningOS.Services;
using Microsoft.AspNetCore.Mvc;

namespace LearningOS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ActivityAuditDto>>> GetAuditHistory([FromQuery] int limit = 50)
    {
        var logs = await _auditService.GetRecentActivityAsync(limit);
        return Ok(logs.Select(l => new ActivityAuditDto
        {
            Id = l.Id,
            ActionType = l.ActionType,
            EntityName = l.EntityName,
            EntityId = l.EntityId,
            Description = l.Description,
            Timestamp = l.Timestamp
        }));
    }
}
