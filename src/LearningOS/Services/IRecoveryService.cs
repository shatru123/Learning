using LearningOS.Dtos;
using LearningOS.Models;

namespace LearningOS.Services;

public interface IRecoveryService
{
    Task<DayPlan> MarkDayMissedAsync(int dayPlanId, MissedDayRequestDto request);
    Task<DayPlan> RecoverDayAsync(int dayPlanId, RecoverDayRequestDto request);
    Task<DayPlan> AddRestDayAsync(AddRestDayRequestDto request);
    Task<DayPlan> AddLeaveDayAsync(AddLeaveDayRequestDto request);
    Task<(int DaysExtended, DateOnly NewEndDate)> ExtendRoadmapAsync(ExtendRoadmapRequestDto request);
    Task<int> GetUnfinishedDaysCountAsync();
    Task<List<RecoveryQueueItemDto>> GetRecoveryQueueAsync();
}
