using LearningOS.Models;

namespace LearningOS.Dtos;

public class DashboardSummaryDto
{
    public int TotalPlannedDays { get; set; } = 100;
    public int CompletedDaysCount { get; set; }
    public int PartiallyCompletedDaysCount { get; set; }
    public int SkippedDaysCount { get; set; }
    public int RestDaysCount { get; set; }
    public int MissedDaysCount { get; set; }
    public int InProgressDaysCount { get; set; }
    public int PlannedDaysRemainingCount { get; set; }

    public int TotalTasksCount { get; set; }
    public int CompletedTasksCount { get; set; }
    public double TaskCompletionPercent { get; set; }

    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }

    public int RecoveryQueueCount { get; set; }
    public List<RecoveryQueueItemDto> RecoveryQueue { get; set; } = new();

    public DayPlanDto? TodayPlan { get; set; }
    public List<ActivityAuditDto> RecentActivity { get; set; } = new();
}

public class RecoveryQueueItemDto
{
    public int DayPlanId { get; set; }
    public int? DayNumber { get; set; }
    public DateOnly CalendarDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? MissedReason { get; set; }
    public string? MissedNote { get; set; }
    public int IncompleteTaskCount { get; set; }
    public int IncompleteEstimatedMinutes { get; set; }
    public List<string> IncompleteTaskTitles { get; set; } = new();
}

public class DayPlanDto
{
    public int Id { get; set; }
    public int? DayNumber { get; set; }
    public bool IsLearningDay { get; set; }
    public DateOnly CalendarDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? ReviewSummary { get; set; }
    public int PhaseId { get; set; }
    public int WeekNumber { get; set; }
    public List<LearningTaskDto> Tasks { get; set; } = new();
    public MissedRecordDto? MissedRecord { get; set; }
    public RestRecordDto? RestRecord { get; set; }
    public LeaveRecordDto? LeaveRecord { get; set; }
    public DailyReviewDto? DailyReview { get; set; }
}

public class LearningTaskDto
{
    public int Id { get; set; }
    public int DayPlanId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public int EstimatedMinutes { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int OriginalDayNumber { get; set; }
    public int CurrentDayNumber { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<TaskHistoryDto> History { get; set; } = new();
}

public class TaskHistoryDto
{
    public int Id { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; }
}

public class MissedRecordDto
{
    public DateOnly MissedDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? RecoveryStrategy { get; set; }
    public DateTime? RecoveredAt { get; set; }
}

public class RestRecordDto
{
    public DateOnly Date { get; set; }
    public string? Reason { get; set; }
    public bool IsPrePlanned { get; set; }
}

public class LeaveRecordDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public bool PreservesStreak { get; set; }
}

public class ActivityAuditDto
{
    public int Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class MissedDayRequestDto
{
    public string Reason { get; set; } = "Other"; // Work, Health, Family, Travel, Personal, Too much workload, Lack of time, Other
    public string? Note { get; set; }
}

public class RecoverDayRequestDto
{
    // Options: MoveToTomorrow, Reschedule, Compress, Skip
    public string Strategy { get; set; } = "MoveToTomorrow";
    public DateOnly? RescheduleDate { get; set; }
    public int CompressDays { get; set; } = 3; // 3..7 days
    public int? MaxExtraMinutesPerDay { get; set; } // override setting if provided
}

public class AddRestDayRequestDto
{
    public DateOnly Date { get; set; }
    public string? Reason { get; set; }
    public bool IsPrePlanned { get; set; } = true;
}

public class AddLeaveDayRequestDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string LeaveType { get; set; } = "Personal work";
    public string? Reason { get; set; }
}

public class ExtendRoadmapRequestDto
{
    public int DaysToExtend { get; set; }
    public string? Reason { get; set; }
}

public class DailyReviewDto
{
    public int DayPlanId { get; set; }
    public DateOnly ReviewDate { get; set; }
    public string Rating { get; set; } = "Completed everything";
    public string? WhatHappenedNotes { get; set; }
    public string? CarryForwardNotes { get; set; }
    public string? TomorrowPriority { get; set; }
}

public class TaskStatusUpdateDto
{
    public string Status { get; set; } = "Completed"; // Pending, InProgress, Completed, Skipped, Rescheduled
    public string? Note { get; set; }
}
