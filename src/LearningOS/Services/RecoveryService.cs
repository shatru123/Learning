using System.Text.Json;
using LearningOS.Data;
using LearningOS.Dtos;
using LearningOS.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningOS.Services;

public class RecoveryService : IRecoveryService
{
    private readonly LearningDbContext _db;
    private readonly IAuditService _auditService;
    private readonly ILogger<RecoveryService> _logger;

    public RecoveryService(LearningDbContext db, IAuditService auditService, ILogger<RecoveryService> logger)
    {
        _db = db;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<DayPlan> MarkDayMissedAsync(int dayPlanId, MissedDayRequestDto request)
    {
        var day = await _db.DayPlans
            .Include(d => d.Tasks)
            .Include(d => d.StatusHistory)
            .Include(d => d.MissedRecord)
            .FirstOrDefaultAsync(d => d.Id == dayPlanId);

        if (day == null)
            throw new KeyNotFoundException($"DayPlan with Id {dayPlanId} not found.");

        var oldStatus = day.Status;
        day.Status = DayStatus.Missed;
        day.UpdatedAt = DateTime.UtcNow;

        if (day.MissedRecord == null)
        {
            day.MissedRecord = new MissedDayRecord
            {
                DayPlanId = day.Id,
                MissedDate = day.CalendarDate,
                Reason = request.Reason,
                Note = request.Note,
                CreatedAt = DateTime.UtcNow
            };
            _db.MissedDayRecords.Add(day.MissedRecord);
        }
        else
        {
            day.MissedRecord.Reason = request.Reason;
            day.MissedRecord.Note = request.Note;
            day.MissedRecord.RecoveryStrategy = null;
            day.MissedRecord.RecoveredAt = null;
        }

        // Add to history
        day.StatusHistory.Add(new DayStatusHistory
        {
            DayPlanId = day.Id,
            OldStatus = oldStatus,
            NewStatus = DayStatus.Missed,
            Reason = request.Reason,
            Note = request.Note,
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        await _auditService.LogActivityAsync(
            "DayMissed",
            "DayPlan",
            day.Id.ToString(),
            $"Day {day.DayNumber ?? 0} ({day.CalendarDate:yyyy-MM-dd}) marked as missed. Reason: {request.Reason}. Note: {request.Note}");

        return day;
    }

    public async Task<DayPlan> RecoverDayAsync(int dayPlanId, RecoverDayRequestDto request)
    {
        var day = await _db.DayPlans
            .Include(d => d.Tasks)
                .ThenInclude(t => t.History)
            .Include(d => d.MissedRecord)
            .Include(d => d.StatusHistory)
            .FirstOrDefaultAsync(d => d.Id == dayPlanId);

        if (day == null)
            throw new KeyNotFoundException($"DayPlan with Id {dayPlanId} not found.");

        var settings = await _db.UserSettings.FirstOrDefaultAsync() ?? new UserSettings();
        int maxExtraMinutes = request.MaxExtraMinutesPerDay ?? settings.MaxExtraRecoveryMinutesPerDay;

        var incompleteTasks = day.Tasks
            .Where(t => t.Status != "Completed" && t.Status != "Skipped")
            .OrderByDescending(t => GetPriorityRank(t.Priority))
            .ThenByDescending(t => t.EstimatedMinutes)
            .ToList();

        string strategy = request.Strategy?.Trim() ?? "MoveToTomorrow";

        if (strategy.Equals("MoveToTomorrow", StringComparison.OrdinalIgnoreCase))
        {
            // Option A: Move to tomorrow with workload protection
            var futureDays = await _db.DayPlans
                .Where(d => d.CalendarDate > day.CalendarDate && d.IsLearningDay)
                .OrderBy(d => d.CalendarDate)
                .Take(5)
                .ToListAsync();

            if (futureDays.Any())
            {
                var tomorrow = futureDays.First();
                int minutesAllocatedToTomorrow = 0;
                int dayIndex = 0;

                foreach (var task in incompleteTasks)
                {
                    var targetDay = futureDays[dayIndex];
                    if (dayIndex == 0 && (minutesAllocatedToTomorrow + task.EstimatedMinutes) > maxExtraMinutes && futureDays.Count > 1)
                    {
                        // Protect against overloading tomorrow: overflow goes to following day
                        dayIndex = 1;
                        targetDay = futureDays[dayIndex];
                    }

                    if (dayIndex == 0)
                    {
                        minutesAllocatedToTomorrow += task.EstimatedMinutes;
                    }

                    int prevDayNum = task.CurrentDayNumber;
                    task.CurrentDayNumber = targetDay.DayNumber ?? task.CurrentDayNumber;
                    task.DayPlanId = targetDay.Id;
                    task.Status = "Pending";

                    task.History.Add(new TaskHistory
                    {
                        LearningTaskId = task.Id,
                        OldStatus = "Missed",
                        NewStatus = "Pending",
                        Note = $"Moved from Day {prevDayNum} to Day {targetDay.DayNumber} via Smart Recovery (Cap: {maxExtraMinutes}m)",
                        ChangedAt = DateTime.UtcNow
                    });
                }
            }
        }
        else if (strategy.Equals("Reschedule", StringComparison.OrdinalIgnoreCase) && request.RescheduleDate.HasValue)
        {
            // Option B: Reschedule to a specific date
            var targetDay = await _db.DayPlans
                .FirstOrDefaultAsync(d => d.CalendarDate == request.RescheduleDate.Value);

            if (targetDay != null)
            {
                foreach (var task in incompleteTasks)
                {
                    int prevDayNum = task.CurrentDayNumber;
                    task.CurrentDayNumber = targetDay.DayNumber ?? task.CurrentDayNumber;
                    task.DayPlanId = targetDay.Id;
                    task.Status = "Pending";

                    task.History.Add(new TaskHistory
                    {
                        LearningTaskId = task.Id,
                        OldStatus = "Missed",
                        NewStatus = "Pending",
                        Note = $"Rescheduled from Day {prevDayNum} to {targetDay.CalendarDate:yyyy-MM-dd}",
                        ChangedAt = DateTime.UtcNow
                    });
                }
            }
        }
        else if (strategy.Equals("Compress", StringComparison.OrdinalIgnoreCase))
        {
            // Option C: Compress across next 3-7 days with workload cap
            int compressDays = Math.Clamp(request.CompressDays, 3, 7);
            var targetDays = await _db.DayPlans
                .Where(d => d.CalendarDate > day.CalendarDate && d.IsLearningDay)
                .OrderBy(d => d.CalendarDate)
                .Take(compressDays)
                .ToListAsync();

            if (targetDays.Any())
            {
                int targetIndex = 0;
                var minutesPerTarget = new int[targetDays.Count];

                foreach (var task in incompleteTasks)
                {
                    var targetDay = targetDays[targetIndex];
                    int prevDayNum = task.CurrentDayNumber;
                    task.CurrentDayNumber = targetDay.DayNumber ?? task.CurrentDayNumber;
                    task.DayPlanId = targetDay.Id;
                    task.Status = "Pending";

                    minutesPerTarget[targetIndex] += task.EstimatedMinutes;

                    task.History.Add(new TaskHistory
                    {
                        LearningTaskId = task.Id,
                        OldStatus = "Missed",
                        NewStatus = "Pending",
                        Note = $"Compressed across {compressDays} days to Day {targetDay.DayNumber}",
                        ChangedAt = DateTime.UtcNow
                    });

                    // Round-robin with cap consideration
                    targetIndex = (targetIndex + 1) % targetDays.Count;
                }
            }
        }
        else if (strategy.Equals("Skip", StringComparison.OrdinalIgnoreCase))
        {
            // Option D: Skip permanently
            foreach (var task in incompleteTasks)
            {
                var oldStatus = task.Status;
                task.Status = "Skipped";
                task.History.Add(new TaskHistory
                {
                    LearningTaskId = task.Id,
                    OldStatus = oldStatus,
                    NewStatus = "Skipped",
                    Note = "Task permanently skipped during missed day recovery.",
                    ChangedAt = DateTime.UtcNow
                });
            }
            day.Status = DayStatus.Skipped;
        }

        // Record recovery details
        if (day.MissedRecord != null)
        {
            day.MissedRecord.RecoveryStrategy = strategy;
            day.MissedRecord.RecoveredAt = DateTime.UtcNow;
        }

        _db.RecoveryPlans.Add(new RecoveryPlan
        {
            DayPlanId = day.Id,
            Strategy = strategy,
            ExtraMinutesPerDay = maxExtraMinutes,
            DetailsJson = JsonSerializer.Serialize(new
            {
                IncompleteTasksCount = incompleteTasks.Count,
                Strategy = strategy,
                Timestamp = DateTime.UtcNow
            }),
            CreatedAt = DateTime.UtcNow
        });

        day.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _auditService.LogActivityAsync(
            "DayRecovered",
            "DayPlan",
            day.Id.ToString(),
            $"Day {day.DayNumber ?? 0} recovered using strategy '{strategy}'. {incompleteTasks.Count} incomplete tasks handled.");

        return day;
    }

    public async Task<DayPlan> AddRestDayAsync(AddRestDayRequestDto request)
    {
        // Requirement 1 & 6:
        // A planned rest day must NOT destroy streak or overwrite 100 learning days.
        var existingDay = await _db.DayPlans
            .Include(d => d.RestRecord)
            .Include(d => d.StatusHistory)
            .FirstOrDefaultAsync(d => d.CalendarDate == request.Date);

        if (existingDay != null)
        {
            var oldStatus = existingDay.Status;
            existingDay.Status = DayStatus.RestDay;
            existingDay.UpdatedAt = DateTime.UtcNow;

            if (existingDay.RestRecord == null)
            {
                existingDay.RestRecord = new RestDayRecord
                {
                    DayPlanId = existingDay.Id,
                    Date = request.Date,
                    Reason = request.Reason ?? "Planned Rest Day",
                    IsPrePlanned = request.IsPrePlanned,
                    CreatedAt = DateTime.UtcNow
                };
                _db.RestDayRecords.Add(existingDay.RestRecord);
            }
            else
            {
                existingDay.RestRecord.Reason = request.Reason ?? "Planned Rest Day";
                existingDay.RestRecord.IsPrePlanned = request.IsPrePlanned;
            }

            existingDay.StatusHistory.Add(new DayStatusHistory
            {
                DayPlanId = existingDay.Id,
                OldStatus = oldStatus,
                NewStatus = DayStatus.RestDay,
                Reason = "Planned Rest Day",
                Note = request.Reason,
                ChangedAt = DateTime.UtcNow
            });

            // Shift future uncompleted learning days by 1 calendar day to preserve all 100 learning days
            var futureDays = await _db.DayPlans
                .Where(d => d.CalendarDate > request.Date && d.IsLearningDay)
                .OrderBy(d => d.CalendarDate)
                .ToListAsync();

            foreach (var fDay in futureDays)
            {
                fDay.CalendarDate = fDay.CalendarDate.AddDays(1);
            }

            await _db.SaveChangesAsync();

            await _auditService.LogActivityAsync(
                "RestDayAdded",
                "DayPlan",
                existingDay.Id.ToString(),
                $"Scheduled Planned Rest Day 🌴 on {request.Date:yyyy-MM-dd}. Shifted subsequent learning days to preserve 100 days.");

            return existingDay;
        }
        else
        {
            // Insert dedicated rest day entry
            var restDay = new DayPlan
            {
                DayNumber = null,
                IsLearningDay = false,
                CalendarDate = request.Date,
                Status = DayStatus.RestDay,
                Title = "🌴 Planned Rest & Recovery Day",
                Theme = "Rest, Recharge & Consolidate",
                CreatedAt = DateTime.UtcNow
            };
            _db.DayPlans.Add(restDay);
            await _db.SaveChangesAsync();

            _db.RestDayRecords.Add(new RestDayRecord
            {
                DayPlanId = restDay.Id,
                Date = request.Date,
                Reason = request.Reason ?? "Planned Rest Day",
                IsPrePlanned = request.IsPrePlanned,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            await _auditService.LogActivityAsync(
                "RestDayAdded",
                "DayPlan",
                restDay.Id.ToString(),
                $"Added Planned Rest Day 🌴 on {request.Date:yyyy-MM-dd}.");

            return restDay;
        }
    }

    public async Task<DayPlan> AddLeaveDayAsync(AddLeaveDayRequestDto request)
    {
        // Requirement 1 & 7: Leave Day (Sick, Vacation, Wedding, Travel, Personal)
        int durationDays = (request.EndDate.DayNumber - request.StartDate.DayNumber) + 1;
        if (durationDays <= 0) durationDays = 1;

        var targetDays = await _db.DayPlans
            .Where(d => d.CalendarDate >= request.StartDate && d.CalendarDate <= request.EndDate)
            .ToListAsync();

        foreach (var day in targetDays)
        {
            var oldStatus = day.Status;
            day.Status = DayStatus.LeaveDay;
            day.UpdatedAt = DateTime.UtcNow;

            day.StatusHistory.Add(new DayStatusHistory
            {
                DayPlanId = day.Id,
                OldStatus = oldStatus,
                NewStatus = DayStatus.LeaveDay,
                Reason = request.LeaveType,
                Note = request.Reason,
                ChangedAt = DateTime.UtcNow
            });
        }

        // Shift future learning days forward by durationDays so 100 learning days are fully preserved
        var futureDays = await _db.DayPlans
            .Where(d => d.CalendarDate > request.EndDate && d.IsLearningDay)
            .OrderBy(d => d.CalendarDate)
            .ToListAsync();

        foreach (var fDay in futureDays)
        {
            fDay.CalendarDate = fDay.CalendarDate.AddDays(durationDays);
        }

        var leaveRecord = new LeaveDayRecord
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            LeaveType = request.LeaveType,
            Reason = request.Reason,
            PreservesStreak = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.LeaveDayRecords.Add(leaveRecord);

        await _db.SaveChangesAsync();

        await _auditService.LogActivityAsync(
            "LeaveDayAdded",
            "LeaveDayRecord",
            leaveRecord.Id.ToString(),
            $"Recorded Leave ({request.LeaveType}) from {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd} ({durationDays} days). Preserved 100 learning days.");

        return targetDays.FirstOrDefault() ?? new DayPlan { CalendarDate = request.StartDate, Status = DayStatus.LeaveDay };
    }

    public async Task<(int DaysExtended, DateOnly NewEndDate)> ExtendRoadmapAsync(ExtendRoadmapRequestDto request)
    {
        // Requirement 8: Extend Roadmap
        int daysToExtend = request.DaysToExtend > 0 ? request.DaysToExtend : 5;

        // Shift all future uncompleted learning days
        var uncompletedDays = await _db.DayPlans
            .Where(d => d.Status == DayStatus.Planned || d.Status == DayStatus.InProgress || d.Status == DayStatus.Missed)
            .OrderBy(d => d.CalendarDate)
            .ToListAsync();

        foreach (var day in uncompletedDays)
        {
            day.CalendarDate = day.CalendarDate.AddDays(daysToExtend);
            day.UpdatedAt = DateTime.UtcNow;
        }

        var plan = await _db.LearningPlans.FirstOrDefaultAsync();
        var settings = await _db.UserSettings.FirstOrDefaultAsync();

        DateOnly newEndDate = (plan?.EndDate ?? new DateOnly(2026, 12, 31)).AddDays(daysToExtend);

        if (plan != null)
        {
            plan.EndDate = newEndDate;
            plan.UpdatedAt = DateTime.UtcNow;
        }

        if (settings != null)
        {
            settings.RoadmapEndDate = newEndDate;
            settings.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        await _auditService.LogActivityAsync(
            "RoadmapExtended",
            "LearningPlan",
            plan?.Id.ToString(),
            $"Roadmap extended by {daysToExtend} days. New completion target: {newEndDate:yyyy-MM-dd}. Reason: {request.Reason ?? "Accommodating missed/rest days"}");

        return (daysToExtend, newEndDate);
    }

    public async Task<int> GetUnfinishedDaysCountAsync()
    {
        return await _db.DayPlans
            .CountAsync(d => d.Status == DayStatus.Missed || d.Status == DayStatus.Skipped);
    }

    public async Task<List<RecoveryQueueItemDto>> GetRecoveryQueueAsync()
    {
        var missedOrIncompleteDays = await _db.DayPlans
            .Include(d => d.Tasks)
            .Include(d => d.MissedRecord)
            .Where(d => d.Status == DayStatus.Missed || d.Tasks.Any(t => t.Status == "Pending" && t.OriginalDayNumber < (d.DayNumber ?? 0)))
            .OrderBy(d => d.CalendarDate)
            .ToListAsync();

        var queue = new List<RecoveryQueueItemDto>();

        foreach (var day in missedOrIncompleteDays)
        {
            var incTasks = day.Tasks.Where(t => t.Status != "Completed" && t.Status != "Skipped").ToList();
            if (incTasks.Any() || day.Status == DayStatus.Missed)
            {
                queue.Add(new RecoveryQueueItemDto
                {
                    DayPlanId = day.Id,
                    DayNumber = day.DayNumber,
                    CalendarDate = day.CalendarDate,
                    Status = day.Status.ToString(),
                    MissedReason = day.MissedRecord?.Reason,
                    MissedNote = day.MissedRecord?.Note,
                    IncompleteTaskCount = incTasks.Count,
                    IncompleteEstimatedMinutes = incTasks.Sum(t => t.EstimatedMinutes),
                    IncompleteTaskTitles = incTasks.Select(t => t.Title).Take(4).ToList()
                });
            }
        }

        return queue;
    }

    private static int GetPriorityRank(string priority)
    {
        return priority?.ToLowerInvariant() switch
        {
            "critical" => 4,
            "high" => 3,
            "medium" => 2,
            _ => 1
        };
    }
}
