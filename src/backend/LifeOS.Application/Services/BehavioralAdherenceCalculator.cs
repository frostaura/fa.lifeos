using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.Interfaces;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeOS.Application.Services;

/// <summary>
/// v3.0: Behavioral Adherence Calculator with streak penalty integration
/// Formula: rawAdherence × 100 × penaltyFactor
/// </summary>
public class BehavioralAdherenceCalculator : IBehavioralAdherenceCalculator
{
    private readonly ILifeOSDbContext _context;
    private readonly ILogger<BehavioralAdherenceCalculator> _logger;
    private const decimal PenaltyScale = 100m; // Maps penalty scores to 0-1 range

    public BehavioralAdherenceCalculator(
        ILifeOSDbContext context,
        ILogger<BehavioralAdherenceCalculator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AdherenceResult> CalculateAsync(
        Guid userId, 
        DateTime? asOfDate = null, 
        int lookbackDays = 7, 
        CancellationToken cancellationToken = default)
    {
        var evaluationDate = asOfDate ?? DateTime.UtcNow;
        var startDate = evaluationDate.AddDays(-lookbackDays);
        
        _logger.LogInformation(
            "Calculating Behavioral Adherence for user {UserId} as of {Date} (lookback: {Days} days)", 
            userId, evaluationDate, lookbackDays);
        
        // 1. Count scheduled tasks in time window
        var scheduledCount = await CountScheduledTasksAsync(userId, startDate, evaluationDate, cancellationToken);
        
        // 2. Count completed tasks in time window
        var completedCount = await CountCompletedTasksAsync(userId, startDate, evaluationDate, cancellationToken);
        
        // 3. Calculate raw adherence (completed / scheduled)
        var rawAdherence = scheduledCount > 0 
            ? (decimal)completedCount / scheduledCount 
            : 0m;
        
        // 4. Calculate average penalty from active streaks
        var avgPenalty = await CalculateAveragePenaltyAsync(userId, cancellationToken);
        
        // 5. Calculate penalty factor (clamped 0.5-1.0)
        var penaltyFactor = CalculatePenaltyFactor(avgPenalty);
        
        // 6. Calculate final score: rawAdherence × 100 × penaltyFactor
        var score = rawAdherence * 100m * penaltyFactor;
        
        _logger.LogInformation(
            "Adherence calculated: Score={Score:F2}, Raw={Raw:F2}, Penalty={Penalty:F2}, " +
            "Scheduled={Scheduled}, Completed={Completed}",
            score, rawAdherence, penaltyFactor, scheduledCount, completedCount);
        
        return new AdherenceResult
        {
            Score = score,
            RawAdherence = rawAdherence,
            PenaltyFactor = penaltyFactor,
            TimeWindowDays = lookbackDays,
            TasksScheduled = scheduledCount,
            TasksCompleted = completedCount,
            CalculatedAt = evaluationDate
        };
    }

    public async Task<AdherenceSnapshot> SaveSnapshotAsync(
        AdherenceResult result, 
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var snapshot = new AdherenceSnapshot
        {
            UserId = userId,
            Timestamp = result.CalculatedAt,
            Score = result.Score,
            TimeWindowDays = result.TimeWindowDays,
            TasksConsidered = result.TasksScheduled,
            TasksCompleted = result.TasksCompleted,
            RawAdherence = result.RawAdherence,
            PenaltyFactor = result.PenaltyFactor,
            CreatedAt = DateTime.UtcNow
        };

        _context.AdherenceSnapshots.Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation(
            "Saved AdherenceSnapshot {Id} for user {UserId} with score {Score:F2}",
            snapshot.Id, userId, snapshot.Score);

        return snapshot;
    }

    private async Task<int> CountScheduledTasksAsync(
        Guid userId, 
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken)
    {
        // Get all active tasks for the user
        var tasks = await _context.Tasks
            .Where(t => t.UserId == userId && 
                       t.IsActive && 
                       t.TaskType == TaskType.Habit)
            .ToListAsync(cancellationToken);
        
        var totalScheduled = 0;
        var days = (int)(endDate - startDate).TotalDays;
        
        foreach (var task in tasks)
        {
            // Check if task is active during this period
            var taskStart = task.StartDate.ToDateTime(TimeOnly.MinValue);
            var taskEnd = task.EndDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MaxValue;
            
            if (taskEnd < startDate || taskStart > endDate)
                continue; // Task not active in this window
            
            // Calculate scheduled instances based on frequency
            var instanceCount = task.Frequency switch
            {
                Frequency.Daily => days,
                Frequency.Weekly => Math.Max(1, days / 7),
                Frequency.Monthly => Math.Max(1, days / 30),
                Frequency.Quarterly => Math.Max(1, days / 90),
                Frequency.Yearly => Math.Max(1, days / 365),
                Frequency.AdHoc => 0, // Ad-hoc tasks don't count as scheduled
                _ => 0
            };
            
            totalScheduled += instanceCount;
        }
        
        _logger.LogDebug(
            "Counted {Count} scheduled tasks for user {UserId} in {Days} day window",
            totalScheduled, userId, days);
        
        return totalScheduled;
    }

    private async Task<int> CountCompletedTasksAsync(
        Guid userId, 
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken)
    {
        var completedCount = await _context.TaskCompletions
            .Where(tc => tc.UserId == userId && 
                        tc.CompletedAt >= startDate && 
                        tc.CompletedAt < endDate)
            .CountAsync(cancellationToken);
        
        _logger.LogDebug(
            "Counted {Count} completed tasks for user {UserId} in date range",
            completedCount, userId);
        
        return completedCount;
    }

    private async Task<decimal> CalculateAveragePenaltyAsync(
        Guid userId, 
        CancellationToken cancellationToken)
    {
        var streaks = await _context.Streaks
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync(cancellationToken);
        
        if (!streaks.Any())
        {
            _logger.LogDebug("No active streaks found for user {UserId}, penalty = 0", userId);
            return 0m;
        }
        
        var avgPenalty = streaks.Average(s => s.RiskPenaltyScore);
        
        _logger.LogDebug(
            "Average penalty for user {UserId}: {Penalty:F2} (from {Count} streaks)",
            userId, avgPenalty, streaks.Count);
        
        // Normalize by penalty scale (100) to get 0-1 range
        return avgPenalty / PenaltyScale;
    }

    private decimal CalculatePenaltyFactor(decimal avgPenalty)
    {
        // penaltyFactor = clamp(1 - avgPenalty, 0.5, 1.0)
        var factor = 1m - avgPenalty;
        var clamped = Math.Clamp(factor, 0.5m, 1m);
        
        _logger.LogDebug(
            "Penalty factor: {Factor:F2} (before clamp: {BeforeClamp:F2}, avgPenalty: {AvgPenalty:F2})",
            clamped, factor, avgPenalty);
        
        return clamped;
    }
}
