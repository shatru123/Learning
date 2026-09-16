using LearningOS.Data;
using LearningOS.Dtos;
using LearningOS.Models;
using LearningOS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningOS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DaysController : ControllerBase
{
    private readonly LearningDbContext _db;
    private readonly IRecoveryService _recoveryService;
    private readonly IAuditService _auditService;

    public DaysController(
        LearningDbContext db,
        IRecoveryService recoveryService,
        IAuditService auditService)
    {
        _db = db;
        _recoveryService = recoveryService;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<List<DayPlanDto>>> GetDays([FromQuery] int? phase, [FromQuery] string? status, [FromQuery] string? search)
    {
        var query = _db.DayPlans
            .Include(d => d.Tasks).ThenInclude(t => t.History)
            .Include(d => d.MissedRecord)
            .Include(d => d.RestRecord)
            .Include(d => d.LeaveRecord)
            .Include(d => d.DailyReview)
            .AsQueryable();

        if (phase.HasValue)
        {
            query = query.Where(d => d.PhaseId == phase.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DayStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(d => d.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d => d.Title.Contains(search) || d.Theme.Contains(search));
        }

        var days = await query
            .OrderBy(d => d.CalendarDate)
            .ThenBy(d => d.DayNumber)
            .ToListAsync();

        return Ok(days.Select(MapDayPlan));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DayPlanDto>> GetDay(int id)
    {
        var day = await _db.DayPlans
            .Include(d => d.Tasks).ThenInclude(t => t.History)
            .Include(d => d.StatusHistory)
            .Include(d => d.MissedRecord)
            .Include(d => d.RestRecord)
            .Include(d => d.LeaveRecord)
            .Include(d => d.DailyReview)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (day == null)
            return NotFound(new { message = $"DayPlan {id} not found." });

        return Ok(MapDayPlan(day));
    }

    [HttpPost("{id}/status")]
    public async Task<ActionResult<DayPlanDto>> UpdateStatus(int id, [FromBody] DayStatus newStatus)
    {
        var day = await _db.DayPlans
            .Include(d => d.StatusHistory)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (day == null)
            return NotFound(new { message = $"DayPlan {id} not found." });

        var oldStatus = day.Status;
        day.Status = newStatus;
        day.UpdatedAt = DateTime.UtcNow;

        day.StatusHistory.Add(new DayStatusHistory
        {
            DayPlanId = day.Id,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Reason = "User manual update",
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        await _auditService.LogActivityAsync(
            "DayStatusUpdated",
            "DayPlan",
            day.Id.ToString(),
            $"Day {day.DayNumber ?? 0} status changed from {oldStatus} to {newStatus}");

        return Ok(MapDayPlan(day));
    }

    [HttpPost("{id}/missed")]
    public async Task<ActionResult<DayPlanDto>> MarkDayMissed(int id, [FromBody] MissedDayRequestDto request)
    {
        try
        {
            var day = await _recoveryService.MarkDayMissedAsync(id, request);
            return Ok(MapDayPlan(day));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"DayPlan {id} not found." });
        }
    }

    [HttpPost("{id}/recover")]
    public async Task<ActionResult<DayPlanDto>> RecoverDay(int id, [FromBody] RecoverDayRequestDto request)
    {
        try
        {
            var day = await _recoveryService.RecoverDayAsync(id, request);
            return Ok(MapDayPlan(day));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"DayPlan {id} not found." });
        }
    }

    [HttpPost("rest")]
    public async Task<ActionResult<DayPlanDto>> AddRestDay([FromBody] AddRestDayRequestDto request)
    {
        var day = await _recoveryService.AddRestDayAsync(request);
        return Ok(MapDayPlan(day));
    }

    [HttpPost("leave")]
    public async Task<ActionResult<DayPlanDto>> AddLeaveDay([FromBody] AddLeaveDayRequestDto request)
    {
        var day = await _recoveryService.AddLeaveDayAsync(request);
        return Ok(MapDayPlan(day));
    }

    [HttpPost("extend")]
    public async Task<ActionResult> ExtendRoadmap([FromBody] ExtendRoadmapRequestDto request)
    {
        var result = await _recoveryService.ExtendRoadmapAsync(request);
        return Ok(new
        {
            message = $"Roadmap extended by {result.DaysExtended} days.",
            newEndDate = result.NewEndDate
        });
    }

    [HttpGet("unfinished-count")]
    public async Task<ActionResult<int>> GetUnfinishedCount()
    {
        int count = await _recoveryService.GetUnfinishedDaysCountAsync();
        return Ok(new { unfinishedDays = count });
    }

    private static DayPlanDto MapDayPlan(DayPlan d)
    {
        return new DayPlanDto
        {
            Id = d.Id,
            DayNumber = d.DayNumber,
            IsLearningDay = d.IsLearningDay,
            CalendarDate = d.CalendarDate,
            Status = d.Status.ToString(),
            Title = d.Title,
            Theme = d.Theme,
            Notes = d.Notes,
            ReviewSummary = d.ReviewSummary,
            PhaseId = d.PhaseId,
            WeekNumber = d.WeekNumber,
            Tasks = d.Tasks.Select(t => new LearningTaskDto
            {
                Id = t.Id,
                DayPlanId = t.DayPlanId,
                Title = t.Title,
                Description = t.Description,
                Category = t.Category,
                EstimatedMinutes = t.EstimatedMinutes,
                Priority = t.Priority,
                Status = t.Status,
                OriginalDayNumber = t.OriginalDayNumber,
                CurrentDayNumber = t.CurrentDayNumber,
                CompletedAt = t.CompletedAt,
                History = t.History.Select(h => new TaskHistoryDto
                {
                    Id = h.Id,
                    OldStatus = h.OldStatus,
                    NewStatus = h.NewStatus,
                    Note = h.Note,
                    ChangedAt = h.ChangedAt
                }).ToList()
            }).ToList(),
            MissedRecord = d.MissedRecord != null ? new MissedRecordDto
            {
                MissedDate = d.MissedRecord.MissedDate,
                Reason = d.MissedRecord.Reason,
                Note = d.MissedRecord.Note,
                RecoveryStrategy = d.MissedRecord.RecoveryStrategy,
                RecoveredAt = d.MissedRecord.RecoveredAt
            } : null,
            RestRecord = d.RestRecord != null ? new RestRecordDto
            {
                Date = d.RestRecord.Date,
                Reason = d.RestRecord.Reason,
                IsPrePlanned = d.RestRecord.IsPrePlanned
            } : null,
            LeaveRecord = d.LeaveRecord != null ? new LeaveRecordDto
            {
                StartDate = d.LeaveRecord.StartDate,
                EndDate = d.LeaveRecord.EndDate,
                LeaveType = d.LeaveRecord.LeaveType,
                Reason = d.LeaveRecord.Reason,
                PreservesStreak = d.LeaveRecord.PreservesStreak
            } : null
        };
    }
}
