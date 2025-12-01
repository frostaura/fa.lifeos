using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Daily job (midnight) to evaluate streak continuity and mark broken streaks
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
        _logger.LogInformation("Starting streak evaluation job");

        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            
            // Get all active streaks
            var activeStreaks = await _context.Streaks
                .Where(s => s.IsActive && s.CurrentStreakLength > 0)
                .ToListAsync(cancellationToken);

            var brokenCount = 0;
            var penalizedCount = 0;

            foreach (var streak in activeStreaks)
            {
                var (shouldReset, penaltyDays) = _streakService.CalculateMissPenalty(streak, today);

                if (shouldReset)
                {
                    // Reset streak entirely
                    streak.CurrentStreakLength = 0;
                    streak.StreakStartDate = null;
                    streak.MissCount = 0;
                    streak.UpdatedAt = DateTime.UtcNow;
                    brokenCount++;
                    
                    _logger.LogDebug("Streak {StreakId} broken for task {TaskId}", streak.Id, streak.TaskId);
                }
                else if (penaltyDays > 0)
                {
                    // Apply penalty but don't reset
                    streak.CurrentStreakLength = Math.Max(0, streak.CurrentStreakLength - penaltyDays);
                    streak.MissCount++;
                    streak.UpdatedAt = DateTime.UtcNow;
                    penalizedCount++;
                    
                    _logger.LogDebug("Streak {StreakId} penalized by {Days} days", streak.Id, penaltyDays);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation(
                "Streak evaluation completed. Evaluated {Total} streaks. Broken: {Broken}, Penalized: {Penalized}",
                activeStreaks.Count, brokenCount, penalizedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Streak evaluation job failed");
            throw;
        }
    }
}
