using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeOS.Infrastructure.BackgroundJobs;

/// <summary>
/// v3.0: Daily job (midnight) to evaluate streak continuity and apply miss penalties
/// </summary>
public class StreakEvaluationJob
{
    private readonly ILifeOSDbContext _context;
    private readonly IStreakService _streakService;
    private readonly ILogger<StreakEvaluationJob> _logger;

    public StreakEvaluationJob(
        ILifeOSDbContext context,
        IStreakService streakService,
        ILogger<StreakEvaluationJob> logger)
    {
        _context = context;
        _streakService = streakService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting v3.0 streak evaluation job");

        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            
            // Get all active streaks
            var activeStreaks = await _context.Streaks
                .Where(s => s.IsActive)
                .Include(s => s.Task)
                .Include(s => s.MetricDefinition)
                .ToListAsync(cancellationToken);

            var missCount = 0;
            var successCount = 0;

            foreach (var streak in activeStreaks)
            {
                // Determine if success occurred today
                // For task-based streaks, check TaskCompletion records
                // For metric-based streaks, check MetricRecord entries
                bool successToday = await HasSuccessToday(streak, today, cancellationToken);

                if (successToday)
                {
                    _streakService.UpdateStreakOnSuccess(streak, today);
                    successCount++;
                    _logger.LogDebug("Streak {StreakId} marked success: ConsecutiveMisses={Misses}, RiskPenalty={Penalty}",
                        streak.Id, streak.ConsecutiveMisses, streak.RiskPenaltyScore);
                }
                else
                {
                    _streakService.UpdateStreakOnMiss(streak, today);
                    missCount++;
                    _logger.LogDebug("Streak {StreakId} marked miss: ConsecutiveMisses={Misses}, RiskPenalty={Penalty}",
                        streak.Id, streak.ConsecutiveMisses, streak.RiskPenaltyScore);
                }

                streak.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation(
                "v3.0 Streak evaluation completed. Evaluated {Total} streaks. Successes: {Success}, Misses: {Miss}",
                activeStreaks.Count, successCount, missCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Streak evaluation job failed");
            throw;
        }
    }

    private async Task<bool> HasSuccessToday(Domain.Entities.Streak streak, DateOnly today, CancellationToken ct)
    {
        // For task-based streaks
        if (streak.TaskId.HasValue)
        {
            var hasCompletion = await _context.TaskCompletions
                .AnyAsync(tc => tc.TaskId == streak.TaskId.Value 
                    && DateOnly.FromDateTime(tc.CompletedAt) == today, ct);
            
            if (hasCompletion) return true;

            // Also check if the task itself is completed today
            var taskCompletedToday = await _context.Tasks
                .AnyAsync(t => t.Id == streak.TaskId.Value 
                    && t.IsCompleted 
                    && t.CompletedAt.HasValue
                    && DateOnly.FromDateTime(t.CompletedAt.Value) == today, ct);

            return taskCompletedToday;
        }

        // For metric-based streaks
        if (!string.IsNullOrEmpty(streak.MetricCode))
        {
            return await _context.MetricRecords
                .AnyAsync(mr => mr.MetricCode == streak.MetricCode
                    && mr.UserId == streak.UserId
                    && DateOnly.FromDateTime(mr.RecordedAt) == today, ct);
        }

        return false;
    }
}
