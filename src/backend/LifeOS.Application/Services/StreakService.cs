using LifeOS.Domain.Entities;

namespace LifeOS.Application.Services;

public interface IStreakService
{
    void UpdateStreak(Streak streak, DateOnly completionDate);
    void ApplyMissPenalty(Streak streak, DateOnly currentDate);
    (bool shouldReset, int penaltyDays) CalculateMissPenalty(Streak streak, DateOnly currentDate);
}

public class StreakService : IStreakService
{
    /// <summary>
    /// Update streak when a task/metric is completed
    /// </summary>
    public void UpdateStreak(Streak streak, DateOnly completionDate)
    {
        if (streak.LastSuccessDate == null)
        {
            // First completion
            streak.CurrentStreakLength = 1;
            streak.LongestStreakLength = 1;
            streak.LastSuccessDate = completionDate;
            streak.StreakStartDate = completionDate;
            streak.MissCount = 0;
        }
        else
        {
            var lastSuccess = streak.LastSuccessDate.Value;
            var daysSinceLastSuccess = completionDate.DayNumber - lastSuccess.DayNumber;

            if (daysSinceLastSuccess == 0)
            {
                // Already completed today - no change to streak
                return;
            }
            else if (daysSinceLastSuccess == 1)
            {
                // Consecutive day - increment streak
                streak.CurrentStreakLength++;
                streak.LastSuccessDate = completionDate;
                streak.MissCount = 0;

                if (streak.CurrentStreakLength > streak.LongestStreakLength)
                {
                    streak.LongestStreakLength = streak.CurrentStreakLength;
                }
            }
            else if (daysSinceLastSuccess <= streak.MaxAllowedMisses + 1)
            {
                // Within single miss grace period
                // Count the missed days
                var missedDays = daysSinceLastSuccess - 1;
                streak.MissCount += missedDays;
                streak.CurrentStreakLength++;
                streak.LastSuccessDate = completionDate;

                if (streak.CurrentStreakLength > streak.LongestStreakLength)
                {
                    streak.LongestStreakLength = streak.CurrentStreakLength;
                }
            }
            else
            {
                // Streak broken - reset
                streak.CurrentStreakLength = 1;
                streak.LastSuccessDate = completionDate;
                streak.StreakStartDate = completionDate;
                streak.MissCount = 0;
            }
        }
    }

    /// <summary>
    /// Calculate if a penalty should be applied for consecutive misses
    /// </summary>
    public (bool shouldReset, int penaltyDays) CalculateMissPenalty(Streak streak, DateOnly currentDate)
    {
        if (streak.LastSuccessDate == null)
        {
            return (false, 0);
        }

        var daysSinceLastSuccess = currentDate.DayNumber - streak.LastSuccessDate.Value.DayNumber;

        // Single miss grace period
        if (daysSinceLastSuccess <= streak.MaxAllowedMisses + 1)
        {
            return (false, 0);
        }

        // Consecutive misses - apply penalty
        var consecutiveMisses = daysSinceLastSuccess - 1;
        
        // Penalty: -2 days per consecutive miss beyond the grace period
        var penaltyDays = Math.Max(0, consecutiveMisses - streak.MaxAllowedMisses) * 2;

        // If penalty exceeds current streak, it should reset
        var shouldReset = penaltyDays >= streak.CurrentStreakLength;

        return (shouldReset, penaltyDays);
    }

    /// <summary>
    /// Apply miss penalty to streak (called by daily background job)
    /// </summary>
    public void ApplyMissPenalty(Streak streak, DateOnly currentDate)
    {
        var (shouldReset, penaltyDays) = CalculateMissPenalty(streak, currentDate);

        if (shouldReset)
        {
            // Reset streak entirely
            streak.CurrentStreakLength = 0;
            streak.StreakStartDate = null;
            streak.MissCount = 0;
        }
        else if (penaltyDays > 0)
        {
            // Apply penalty but don't reset
            streak.CurrentStreakLength = Math.Max(0, streak.CurrentStreakLength - penaltyDays);
            streak.MissCount++;
        }
    }
}
