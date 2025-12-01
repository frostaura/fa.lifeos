using LifeOS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Services;

public interface IScoreCalculator
{
    Task<decimal> CalculateDimensionScoreAsync(Guid userId, Guid dimensionId, CancellationToken cancellationToken = default);
    Task<decimal> CalculateLifeScoreAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class ScoreCalculator : IScoreCalculator
{
    private readonly ILifeOSDbContext _context;

    public ScoreCalculator(ILifeOSDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Calculate a dimension score (0-100) based on:
    /// - Task/habit completion rate
    /// - Metric values relative to targets
    /// - Streak consistency
    /// </summary>
    public async Task<decimal> CalculateDimensionScoreAsync(Guid userId, Guid dimensionId, CancellationToken cancellationToken = default)
    {
        var scoreComponents = new List<decimal>();

        // 1. Habit completion rate (last 7 days)
        var habitScore = await CalculateHabitCompletionScoreAsync(userId, dimensionId, cancellationToken);
        if (habitScore.HasValue)
            scoreComponents.Add(habitScore.Value);

        // 2. Metric health score (based on recent metrics vs targets)
        var metricScore = await CalculateMetricHealthScoreAsync(userId, dimensionId, cancellationToken);
        if (metricScore.HasValue)
            scoreComponents.Add(metricScore.Value);

        // 3. Streak bonus (active streaks boost score)
        var streakBonus = await CalculateStreakBonusAsync(userId, dimensionId, cancellationToken);

        // Calculate average with streak bonus
        var baseScore = scoreComponents.Count > 0 
            ? scoreComponents.Average() 
            : 50m; // Default to 50 if no data

        var finalScore = Math.Min(100, baseScore + streakBonus);
        return Math.Round(finalScore, 1);
    }

    /// <summary>
    /// Calculate life score as weighted average of dimension scores
    /// </summary>
    public async Task<decimal> CalculateLifeScoreAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var dimensions = await _context.Dimensions
            .Where(d => d.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        decimal weightedSum = 0;
        decimal totalWeight = 0;

        foreach (var dimension in dimensions)
        {
            var score = await CalculateDimensionScoreAsync(userId, dimension.Id, cancellationToken);
            weightedSum += score * dimension.DefaultWeight;
            totalWeight += dimension.DefaultWeight;
        }

        return totalWeight > 0 
            ? Math.Round(weightedSum / totalWeight, 1) 
            : 50m;
    }

    private async Task<decimal?> CalculateHabitCompletionScoreAsync(Guid userId, Guid dimensionId, CancellationToken cancellationToken)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        // Get habits for this dimension
        var habits = await _context.Tasks
            .Where(t => t.UserId == userId 
                && t.DimensionId == dimensionId 
                && t.TaskType == Domain.Enums.TaskType.Habit 
                && t.IsActive)
            .ToListAsync(cancellationToken);

        if (!habits.Any())
            return null;

        // Calculate expected completions vs actual (simple daily check)
        var completedHabits = habits.Where(h => 
            h.CompletedAt.HasValue && h.CompletedAt.Value >= sevenDaysAgo);

        // For now, simple ratio based on recent completions
        var completionRate = (decimal)completedHabits.Count() / habits.Count;
        return completionRate * 100;
    }

    private async Task<decimal?> CalculateMetricHealthScoreAsync(Guid userId, Guid dimensionId, CancellationToken cancellationToken)
    {
        // Get metrics for this dimension
        var metricDefs = await _context.MetricDefinitions
            .Where(m => m.DimensionId == dimensionId && m.IsActive)
            .ToListAsync(cancellationToken);

        if (!metricDefs.Any())
            return null;

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var scores = new List<decimal>();

        foreach (var def in metricDefs)
        {
            var recentValue = await _context.MetricRecords
                .Where(r => r.UserId == userId 
                    && r.MetricCode == def.Code 
                    && r.RecordedAt >= thirtyDaysAgo)
                .OrderByDescending(r => r.RecordedAt)
                .Select(r => r.ValueNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (recentValue.HasValue && def.MinValue.HasValue && def.MaxValue.HasValue)
            {
                // Normalize to 0-100 based on min/max bounds
                var range = def.MaxValue.Value - def.MinValue.Value;
                if (range > 0)
                {
                    var normalized = ((recentValue.Value - def.MinValue.Value) / range) * 100;
                    normalized = Math.Max(0, Math.Min(100, normalized));
                    scores.Add(normalized);
                }
            }
            else if (recentValue.HasValue)
            {
                // If no bounds, assume 50 (neutral)
                scores.Add(50);
            }
        }

        return scores.Count > 0 ? scores.Average() : null;
    }

    private async Task<decimal> CalculateStreakBonusAsync(Guid userId, Guid dimensionId, CancellationToken cancellationToken)
    {
        // Get tasks for this dimension with active streaks
        var taskIds = await _context.Tasks
            .Where(t => t.UserId == userId && t.DimensionId == dimensionId && t.IsActive)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var streaks = await _context.Streaks
            .Where(s => s.UserId == userId 
                && s.TaskId.HasValue 
                && taskIds.Contains(s.TaskId.Value) 
                && s.IsActive 
                && s.CurrentStreakLength > 0)
            .ToListAsync(cancellationToken);

        if (!streaks.Any())
            return 0;

        // Bonus: 1 point per streak day (max 10 bonus points)
        var totalStreakDays = streaks.Sum(s => s.CurrentStreakLength);
        return Math.Min(10, totalStreakDays);
    }
}
