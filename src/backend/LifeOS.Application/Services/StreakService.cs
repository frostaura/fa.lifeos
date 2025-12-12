using LifeOS.Domain.Entities;

namespace LifeOS.Application.Services;

public interface IStreakService
{
    void UpdateStreakOnSuccess(Streak streak, DateOnly completionDate);
    void UpdateStreakOnMiss(Streak streak, DateOnly missDate);
    void EvaluateStreakStatus(Streak streak, bool success);
}

public class StreakService : IStreakService
{
    /// <summary>
    /// v3.0: Update streak when a success occurs
    /// Implements success decay: riskPenaltyScore = max(0, riskPenaltyScore - 2)
    /// </summary>
    public void UpdateStreakOnSuccess(Streak streak, DateOnly completionDate)
    {
        // Increment streak
        streak.CurrentStreakLength++;
        streak.LastSuccessDate = completionDate;
        
        // Reset consecutive misses
        streak.ConsecutiveMisses = 0;
        
        // Apply success decay to penalty score
        streak.RiskPenaltyScore = Math.Max(0, streak.RiskPenaltyScore - 2);
        
        // Update longest streak if needed
        if (streak.CurrentStreakLength > streak.LongestStreakLength)
        {
            streak.LongestStreakLength = streak.CurrentStreakLength;
        }
        
        // Track penalty calculation time
        streak.LastPenaltyCalculatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// v3.0: Update streak when a miss occurs
    /// Implements forgiving first miss logic with escalating penalties
    /// </summary>
    public void UpdateStreakOnMiss(Streak streak, DateOnly missDate)
    {
        if (streak.ConsecutiveMisses == 0)
        {
            // First consecutive miss - forgiving (no penalty)
            streak.ConsecutiveMisses = 1;
            // riskPenaltyScore unchanged
        }
        else if (streak.ConsecutiveMisses == 1)
        {
            // Second consecutive miss - apply initial penalty
            streak.ConsecutiveMisses = 2;
            streak.RiskPenaltyScore = 5;
        }
        else
        {
            // Third+ consecutive miss - escalating penalty
            streak.ConsecutiveMisses++;
            streak.RiskPenaltyScore = 10 * (streak.ConsecutiveMisses - 1);
        }
        
        // Update longest streak if current was better
        if (streak.CurrentStreakLength > streak.LongestStreakLength)
        {
            streak.LongestStreakLength = streak.CurrentStreakLength;
        }
        
        // Reset current streak
        streak.CurrentStreakLength = 0;
        
        // Track penalty calculation time
        streak.LastPenaltyCalculatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// v3.0: Main entry point for streak evaluation (called by daily job or on-demand)
    /// Determines success/failure and applies appropriate logic
    /// </summary>
    public void EvaluateStreakStatus(Streak streak, bool success)
    {
        if (success)
        {
            UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow));
        }
        else
        {
            UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow));
        }
    }
}
