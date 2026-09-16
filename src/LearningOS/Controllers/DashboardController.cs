using LearningOS.Data;
using LearningOS.Dtos;
using LearningOS.Models;
using LearningOS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningOS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly LearningDbContext _db;
    private readonly IStreakService _streakService;
    private readonly IRecoveryService _recoveryService;
    private readonly IAuditService _auditService;

    public DashboardController(
        LearningDbContext db,
        IStreakService streakService,
        IRecoveryService recoveryService,
        IAuditService auditService)
    {
        _db = db;
        _streakService = streakService;
        _recoveryService = recoveryService;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var dayPlans = await _db.DayPlans
            .Include(d => d.Tasks).ThenInclude(t => t.History)
            .Include(d => d.MissedRecord)
            .Include(d => d.RestRecord)
            .Include(d => d.LeaveRecord)
            .Include(d => d.DailyReview)
            .OrderBy(d => d.CalendarDate)
            .ToListAsync();

        var (currentStreak, bestStreak) = _streakService.CalculateStreak(dayPlans);
        var recoveryQueue = await _recoveryService.GetRecoveryQueueAsync();

        int completedDays = dayPlans.Count(d => d.Status == DayStatus.Completed);
        int partialDays = dayPlans.Count(d => d.Status == DayStatus.PartiallyCompleted);
        int skippedDays = dayPlans.Count(d => d.Status == DayStatus.Skipped);
        int restDays = dayPlans.Count(d => d.Status == DayStatus.RestDay);
        int missedDays = dayPlans.Count(d => d.Status == DayStatus.Missed);
        int inProgressDays = dayPlans.Count(d => d.Status == DayStatus.InProgress);
        int plannedRemaining = dayPlans.Count(d => d.Status == DayStatus.Planned);

        var allTasks = dayPlans.SelectMany(d => d.Tasks).ToList();
        int totalTasks = allTasks.Count;
        int completedTasks = allTasks.Count(t => t.Status == "Completed");
        double taskCompletionPercent = totalTasks > 0 ? Math.Round((double)completedTasks / totalTasks * 100, 1) : 0;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayPlan = dayPlans.FirstOrDefault(d => d.CalendarDate == today) 
                     ?? dayPlans.FirstOrDefault(d => d.Status == DayStatus.InProgress)
                     ?? dayPlans.FirstOrDefault(d => d.Status == DayStatus.Planned);

        var recentLogs = await _auditService.GetRecentActivityAsync(10);

        var summary = new DashboardSummaryDto
        {
            TotalPlannedDays = 100,
            CompletedDaysCount = completedDays,
            PartiallyCompletedDaysCount = partialDays,
            SkippedDaysCount = skippedDays,
            RestDaysCount = restDays,
            MissedDaysCount = missedDays,
            InProgressDaysCount = inProgressDays,
            PlannedDaysRemainingCount = plannedRemaining,
            TotalTasksCount = totalTasks,
            CompletedTasksCount = completedTasks,
            TaskCompletionPercent = taskCompletionPercent,
            CurrentStreak = currentStreak,
            BestStreak = bestStreak,
            RecoveryQueueCount = recoveryQueue.Count,
            RecoveryQueue = recoveryQueue,
            TodayPlan = todayPlan != null ? MapDayPlan(todayPlan) : null,
            RecentActivity = recentLogs.Select(l => new ActivityAuditDto
            {
                Id = l.Id,
                ActionType = l.ActionType,
                EntityName = l.EntityName,
                EntityId = l.EntityId,
                Description = l.Description,
                Timestamp = l.Timestamp
            }).ToList()
        };

        return Ok(summary);
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
                CompletedAt = t.CompletedAt
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
