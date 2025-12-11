using LifeOS.Application.Common.Interfaces;
using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Services;

/// <summary>
/// v1.2: Behavioral Adherence calculation service
/// Calculates adherence score based on task completion rate
/// </summary>
public interface IAdherenceService
{
    Task<AdherenceSnapshot> CalculateAdherenceAsync(Guid userId, int timeWindowDays = 7, CancellationToken cancellationToken = default);
}

public class AdherenceService : IAdherenceService
{
    private readonly ILifeOSDbContext _context;

    public AdherenceService(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<AdherenceSnapshot> CalculateAdherenceAsync(Guid userId, int timeWindowDays = 7, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-timeWindowDays);

        // Get all active tasks/habits for the user
        var activeTasks = await _context.Tasks
            .Where(t => t.UserId == userId && t.IsActive)
            .ToListAsync(cancellationToken);

        if (!activeTasks.Any())
        {
            return new AdherenceSnapshot
            {
                UserId = userId,
                Score = 100m,
                TimeWindowDays = timeWindowDays,
                TasksConsidered = 0,
                TasksCompleted = 0
            };
        }

        // Count expected task occurrences based on frequency
        int expectedOccurrences = 0;
        int completedOccurrences = 0;

        foreach (var task in activeTasks)
        {
            int expectedCount = CalculateExpectedOccurrences(task.Frequency, timeWindowDays);
            expectedOccurrences += expectedCount;

            // Count actual completions in the time window
            var completions = await _context.TaskCompletions
                .Where(tc => tc.TaskId == task.Id && tc.CompletedAt >= cutoffDate)
                .CountAsync(cancellationToken);

            completedOccurrences += Math.Min(completions, expectedCount);
        }

        decimal score = expectedOccurrences > 0 
            ? (decimal)completedOccurrences / expectedOccurrences * 100m 
            : 100m;

        var snapshot = new AdherenceSnapshot
        {
            UserId = userId,
            Score = Math.Round(score, 2),
            TimeWindowDays = timeWindowDays,
            TasksConsidered = expectedOccurrences,
            TasksCompleted = completedOccurrences
        };

        _context.AdherenceSnapshots.Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken);

        return snapshot;
    }

    private int CalculateExpectedOccurrences(Domain.Enums.Frequency frequency, int days)
    {
        return frequency switch
        {
            Domain.Enums.Frequency.Daily => days,
            Domain.Enums.Frequency.Weekly => days / 7,
            Domain.Enums.Frequency.Monthly => days / 30,
            Domain.Enums.Frequency.Quarterly => days / 90,
            Domain.Enums.Frequency.Yearly => days / 365,
            _ => 0
        };
    }
}
