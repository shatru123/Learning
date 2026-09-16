using LearningOS.Data;
using LearningOS.Dtos;
using LearningOS.Models;
using LearningOS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningOS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly LearningDbContext _db;
    private readonly IAuditService _auditService;

    public TasksController(LearningDbContext db, IAuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LearningTaskDto>> GetTask(int id)
    {
        var task = await _db.LearningTasks
            .Include(t => t.History)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
            return NotFound(new { message = $"Task {id} not found." });

        return Ok(MapTask(task));
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<LearningTaskDto>> UpdateStatus(int id, [FromBody] TaskStatusUpdateDto request)
    {
        var task = await _db.LearningTasks
            .Include(t => t.History)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
            return NotFound(new { message = $"Task {id} not found." });

        var oldStatus = task.Status;
        task.Status = request.Status;

        if (request.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
        {
            task.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            task.CompletedAt = null;
        }

        // Enforce Requirement 4 & 14: Never lose task history
        task.History.Add(new TaskHistory
        {
            LearningTaskId = task.Id,
            OldStatus = oldStatus,
            NewStatus = request.Status,
            Note = request.Note ?? $"Status changed from {oldStatus} to {request.Status}",
            ChangedAt = DateTime.UtcNow
        });

        // Check and update parent day status
        var day = await _db.DayPlans
            .Include(d => d.Tasks)
            .FirstOrDefaultAsync(d => d.Id == task.DayPlanId);

        if (day != null)
        {
            int completedCount = day.Tasks.Count(t => t.Status == "Completed");
            int totalCount = day.Tasks.Count;

            if (completedCount == totalCount && totalCount > 0 && day.Status != DayStatus.Completed)
            {
                var oldDayStatus = day.Status;
                day.Status = DayStatus.Completed;
                day.UpdatedAt = DateTime.UtcNow;
                day.StatusHistory.Add(new DayStatusHistory
                {
                    DayPlanId = day.Id,
                    OldStatus = oldDayStatus,
                    NewStatus = DayStatus.Completed,
                    Reason = "All tasks completed",
                    ChangedAt = DateTime.UtcNow
                });
            }
            else if (completedCount > 0 && completedCount < totalCount && day.Status == DayStatus.Planned)
            {
                day.Status = DayStatus.PartiallyCompleted;
                day.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        await _auditService.LogActivityAsync(
            "TaskStatusChanged",
            "LearningTask",
            task.Id.ToString(),
            $"Task '{task.Title}' changed to {task.Status}. Day {task.CurrentDayNumber}.");

        return Ok(MapTask(task));
    }

    [HttpPost]
    public async Task<ActionResult<LearningTaskDto>> CreateTask([FromBody] LearningTask task)
    {
        task.CreatedAt = DateTime.UtcNow;
        task.Status = "Pending";
        task.History.Add(new TaskHistory
        {
            OldStatus = "None",
            NewStatus = "Pending",
            Note = "Task created",
            ChangedAt = DateTime.UtcNow
        });

        _db.LearningTasks.Add(task);
        await _db.SaveChangesAsync();

        await _auditService.LogActivityAsync(
            "TaskCreated",
            "LearningTask",
            task.Id.ToString(),
            $"New task added: '{task.Title}' for Day {task.CurrentDayNumber}");

        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, MapTask(task));
    }

    private static LearningTaskDto MapTask(LearningTask t)
    {
        return new LearningTaskDto
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
        };
    }
}
