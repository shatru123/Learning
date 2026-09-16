using LearningOS.Models;
using LearningOS.Services;
using Xunit;

namespace LearningOS.Tests;

public class StreakCalculationTests
{
    private readonly StreakService _streakService = new();

    [Fact]
    public void CompletedDays_IncrementStreak()
    {
        var days = new List<DayPlan>
        {
            new() { DayNumber = 1, CalendarDate = new DateOnly(2026, 9, 21), Status = DayStatus.Completed },
            new() { DayNumber = 2, CalendarDate = new DateOnly(2026, 9, 22), Status = DayStatus.Completed },
            new() { DayNumber = 3, CalendarDate = new DateOnly(2026, 9, 23), Status = DayStatus.Completed }
        };

        var (current, best) = _streakService.CalculateStreak(days);
        Assert.Equal(3, current);
        Assert.Equal(3, best);
    }

    [Fact]
    public void PlannedRestDay_PreservesStreak_DoesNotBreakIt()
    {
        // Requirement 6 & 10:
        // Day 10 ✅, Day 11 ✅, Day 12 🌴 Rest, Day 13 ✅ -> Streak is 3!
        var days = new List<DayPlan>
        {
            new() { DayNumber = 10, CalendarDate = new DateOnly(2026, 10, 1), Status = DayStatus.Completed },
            new() { DayNumber = 11, CalendarDate = new DateOnly(2026, 10, 2), Status = DayStatus.Completed },
            new() { DayNumber = null, CalendarDate = new DateOnly(2026, 10, 3), Status = DayStatus.RestDay },
            new() { DayNumber = 12, CalendarDate = new DateOnly(2026, 10, 4), Status = DayStatus.Completed }
        };

        var (current, best) = _streakService.CalculateStreak(days);
        Assert.Equal(3, current);
        Assert.Equal(3, best);
    }

    [Fact]
    public void RecordedLeaveDay_PreservesStreak_DoesNotBreakIt()
    {
        var days = new List<DayPlan>
        {
            new() { DayNumber = 1, CalendarDate = new DateOnly(2026, 9, 21), Status = DayStatus.Completed },
            new() { DayNumber = 2, CalendarDate = new DateOnly(2026, 9, 22), Status = DayStatus.Completed },
            new() { DayNumber = null, CalendarDate = new DateOnly(2026, 9, 23), Status = DayStatus.LeaveDay },
            new() { DayNumber = null, CalendarDate = new DateOnly(2026, 9, 24), Status = DayStatus.LeaveDay },
            new() { DayNumber = 3, CalendarDate = new DateOnly(2026, 9, 25), Status = DayStatus.Completed }
        };

        var (current, best) = _streakService.CalculateStreak(days);
        Assert.Equal(3, current);
        Assert.Equal(3, best);
    }

    [Fact]
    public void UnexplainedMissedDay_BreaksStreak()
    {
        var days = new List<DayPlan>
        {
            new() { DayNumber = 1, CalendarDate = new DateOnly(2026, 9, 21), Status = DayStatus.Completed },
            new() { DayNumber = 2, CalendarDate = new DateOnly(2026, 9, 22), Status = DayStatus.Completed },
            new() { DayNumber = 3, CalendarDate = new DateOnly(2026, 9, 23), Status = DayStatus.Missed },
            new() { DayNumber = 4, CalendarDate = new DateOnly(2026, 9, 24), Status = DayStatus.Completed }
        };

        var (current, best) = _streakService.CalculateStreak(days);
        // Latest active period is only Day 4 (1 day streak)
        Assert.Equal(1, current);
        // Best historical streak was Day 1 + Day 2 = 2
        Assert.Equal(2, best);
    }
}
