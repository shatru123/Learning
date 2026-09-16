using LearningOS.Models;

namespace LearningOS.Services;

public interface IStreakService
{
    (int CurrentStreak, int BestStreak) CalculateStreak(IEnumerable<DayPlan> dayPlans);
}
