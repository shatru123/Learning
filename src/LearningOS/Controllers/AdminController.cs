using LearningOS.Data;
using LearningOS.Dtos;
using LearningOS.Models;
using LearningOS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningOS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly LearningDbContext _db;
    private readonly ICurriculumProvisioningService _provisioningService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        LearningDbContext db,
        ICurriculumProvisioningService provisioningService,
        ICurrentUserService currentUserService,
        ILogger<AdminController> logger)
    {
        _db = db;
        _provisioningService = provisioningService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet("learners")]
    public async Task<ActionResult<List<LearnerSummaryDto>>> GetLearners()
    {
        var users = await _db.Users.IgnoreQueryFilters()
            .OrderByDescending(u => u.Role == "Admin")
            .ThenByDescending(u => u.CreatedAt)
            .ToListAsync();

        var summaries = new List<LearnerSummaryDto>();

        foreach (var u in users)
        {
            var userDays = await _db.DayPlans.IgnoreQueryFilters()
                .Where(d => d.UserId == u.Id)
                .Include(d => d.Tasks)
                .AsNoTracking()
                .ToListAsync();

            int completedDays = userDays.Count(d => d.Status == DayStatus.Completed);
            int missedDays = userDays.Count(d => d.Status == DayStatus.Missed);
            int totalTasks = userDays.SelectMany(d => d.Tasks).Count();
            int completedTasks = userDays.SelectMany(d => d.Tasks).Count(t => t.Status == "Completed");
            int incompleteTasks = totalTasks - completedTasks;

            var activeDay = userDays
                .Where(d => d.IsLearningDay)
                .OrderBy(d => d.DayNumber)
                .FirstOrDefault(d => d.Status != DayStatus.Completed && d.Status != DayStatus.Skipped);

            // Compute basic streak: contiguous completed days up to latest
            int streak = 0;
            var orderedLearningDays = userDays.Where(d => d.IsLearningDay).OrderBy(d => d.DayNumber).ToList();
            foreach (var d in orderedLearningDays)
            {
                if (d.Status == DayStatus.Completed) streak++;
                else break;
            }

            var lastActiveLog = await _db.ActivityAuditLogs.IgnoreQueryFilters()
                .Where(a => a.UserId == u.Id)
                .OrderByDescending(a => a.Timestamp)
                .Select(a => (DateTime?)a.Timestamp)
                .FirstOrDefaultAsync();

            summaries.Add(new LearnerSummaryDto
            {
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                Status = u.Status,
                StartDate = u.RequestedStartDate,
                CurrentDayNumber = activeDay?.DayNumber,
                CompletedDaysCount = completedDays,
                TotalDaysCount = 100,
                CompletionPercent = userDays.Count > 0 ? Math.Round((double)completedDays / 100.0 * 100.0, 1) : 0,
                CurrentStreak = streak,
                MissedDaysCount = missedDays,
                IncompleteTasksCount = incompleteTasks,
                LastActiveAt = lastActiveLog ?? u.LastLoginAt ?? u.CreatedAt,
                CreatedAt = u.CreatedAt
            });
        }

        return Ok(summaries);
    }

    [HttpGet("pending")]
    public async Task<ActionResult<List<UserProfileDto>>> GetPendingUsers()
    {
        var pending = await _db.Users.IgnoreQueryFilters()
            .Where(u => u.Status == "PendingApproval")
            .OrderBy(u => u.CreatedAt)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                Email = u.Email,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role,
                Status = u.Status,
                RequestedStartDate = u.RequestedStartDate,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(pending);
    }

    [HttpPost("approve/{userId}")]
    public async Task<IActionResult> ApproveUser(int userId)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound(new { message = "User not found." });

        if (user.Status == "Active")
            return BadRequest(new { message = "User is already active." });

        user.Status = "Active";
        user.ApprovedAt = DateTime.UtcNow;
        user.ApprovedByUserId = _currentUserService.UserId;
        await _db.SaveChangesAsync();

        // Provision their 100-day schedule with their chosen start date
        await _provisioningService.ProvisionCurriculumForUserAsync(user.Id, user.RequestedStartDate);

        _logger.LogInformation("Admin approved and provisioned user {Email} (ID: {Id})", user.Email, user.Id);
        return Ok(new { message = $"User {user.FullName} ({user.Email}) has been approved and their 100-day curriculum is provisioned!" });
    }

    [HttpPost("reject/{userId}")]
    public async Task<IActionResult> RejectUser(int userId)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound(new { message = "User not found." });

        user.Status = "Rejected";
        await _db.SaveChangesAsync();

        _logger.LogInformation("Admin rejected user {Email} (ID: {Id})", user.Email, user.Id);
        return Ok(new { message = $"User {user.FullName} registration has been rejected." });
    }

    [HttpGet("invites")]
    public async Task<ActionResult<List<InviteCode>>> GetInvites()
    {
        var invites = await _db.InviteCodes.IgnoreQueryFilters()
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return Ok(invites);
    }

    [HttpPost("invites")]
    public async Task<ActionResult<InviteCode>> CreateInvite([FromBody] CreateInviteCodeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
            return BadRequest(new { message = "Invite code string is required." });

        var codeClean = dto.Code.Trim().ToUpperInvariant();
        if (await _db.InviteCodes.IgnoreQueryFilters().AnyAsync(i => i.Code.ToUpper() == codeClean))
            return BadRequest(new { message = "An invite code with this text already exists." });

        var adminId = _currentUserService.UserId ?? 1;

        var invite = new InviteCode
        {
            Code = codeClean,
            Description = dto.Description,
            MaxUses = dto.MaxUses > 0 ? dto.MaxUses : 10,
            UsedCount = 0,
            ExpiresAt = dto.ExpiresAt,
            CreatedByUserId = adminId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.InviteCodes.Add(invite);
        await _db.SaveChangesAsync();

        return Ok(invite);
    }

    [HttpDelete("invites/{id}")]
    public async Task<IActionResult> DeactivateInvite(int id)
    {
        var invite = await _db.InviteCodes.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == id);
        if (invite == null) return NotFound();

        invite.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Invite code deactivated." });
    }

    [HttpGet("learners/{userId}/roadmap")]
    public async Task<ActionResult<List<DayPlanDto>>> GetLearnerRoadmap(int userId)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound(new { message = "Learner not found." });

        var days = await _db.DayPlans.IgnoreQueryFilters()
            .Where(d => d.UserId == userId)
            .Include(d => d.Tasks)
            .Include(d => d.DailyReview)
            .Include(d => d.MissedRecord)
            .OrderBy(d => d.CalendarDate)
            .ThenBy(d => d.DayNumber)
            .ToListAsync();

        var dtos = days.Select(d => new DayPlanDto
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
                Title = t.Title,
                Description = t.Description,
                Category = t.Category,
                EstimatedMinutes = t.EstimatedMinutes,
                Priority = t.Priority,
                Status = t.Status,
                OriginalDayNumber = t.OriginalDayNumber,
                CurrentDayNumber = t.CurrentDayNumber
            }).ToList()
        }).ToList();

        return Ok(dtos);
    }
}
