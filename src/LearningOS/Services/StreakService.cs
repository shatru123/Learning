using LearningOS.Models;

namespace LearningOS.Services;

public class StreakService : IStreakService
{
    public (int CurrentStreak, int BestStreak) CalculateStreak(IEnumerable<DayPlan> dayPlans)
    {
        var sortedDays = dayPlans
            .OrderBy(d => d.CalendarDate)
            .ToList();

        if (!sortedDays.Any())
            return (0, 0);

        int bestStreak = 0;
        int runningStreak = 0;

        foreach (var day in sortedDays)
        {
            // Future or unstarted planned days do not terminate historical streak calculation
            if (day.Status == DayStatus.Planned)
            {
                continue;
            }

            if (day.Status == DayStatus.Completed || day.Status == DayStatus.PartiallyCompleted)
            {
                runningStreak++;
                if (runningStreak > bestStreak)
                    bestStreak = runningStreak;
            }
            else if (day.Status == DayStatus.RestDay || day.Status == DayStatus.LeaveDay)
            {
                // Planned Rest Day or Leave Day preserves the streak (does not break it, does not increment active learning count)
                // It bridges between learning days!
            }
            else if (day.Status == DayStatus.Missed || day.Status == DayStatus.Skipped)
            {
                // Unexplained missed day breaks the streak
                runningStreak = 0;
            }
        }

        // Calculate current active streak ending at the latest active day
        // Scan backwards from today / latest non-planned day
        int currentStreak = 0;
        var pastDaysReversed = sortedDays
            .Where(d => d.Status != DayStatus.Planned)
            .OrderByDescending(d => d.CalendarDate)
            .ToList();

        foreach (var day in pastDaysReversed)
        {
            if (day.Status == DayStatus.Completed || day.Status == DayStatus.PartiallyCompleted)
            {
                currentStreak++;
            }
            else if (day.Status == DayStatus.RestDay || day.Status == DayStatus.LeaveDay)
            {
                // Preserved bridge, keep going backwards to count connected learning days
                continue;
            }
            else
            {
                // Encountered Missed or Skipped day: streak broken
                break;
            }
        }

        return (currentStreak, Math.Max(bestStreak, currentStreak));
    }
}
