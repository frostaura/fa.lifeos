using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Tasks;
using LifeOS.Application.Services;
using LifeOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Tasks;

public class CompleteTaskCommandHandler : IRequestHandler<CompleteTaskCommand, TaskCompletionResponse?>
{
    private readonly ILifeOSDbContext _context;
    private readonly IStreakService _streakService;

    public CompleteTaskCommandHandler(ILifeOSDbContext context, IStreakService streakService)
    {
        _context = context;
        _streakService = streakService;
    }

    public async Task<TaskCompletionResponse?> Handle(CompleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.Tasks
            .Include(t => t.Streaks)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.UserId == request.UserId, cancellationToken);

        if (task == null)
            return null;

        var completedAt = request.CompletedAt ?? DateTime.UtcNow;
        task.IsCompleted = true;
        task.CompletedAt = completedAt;

        // Update streak
        var streak = task.Streaks.FirstOrDefault(s => s.IsActive);
        int currentStreak = 0;
        int longestStreak = 0;

        if (streak != null)
        {
            _streakService.UpdateStreak(streak, DateOnly.FromDateTime(completedAt));
            currentStreak = streak.CurrentStreakLength;
            longestStreak = streak.LongestStreakLength;
        }
        else if (task.TaskType == LifeOS.Domain.Enums.TaskType.Habit)
        {
            // Create streak if it doesn't exist
            streak = new Streak
            {
                UserId = request.UserId,
                TaskId = task.Id,
                CurrentStreakLength = 1,
                LongestStreakLength = 1,
                LastSuccessDate = DateOnly.FromDateTime(completedAt),
                StreakStartDate = DateOnly.FromDateTime(completedAt),
                IsActive = true
            };
            _context.Streaks.Add(streak);
            currentStreak = 1;
            longestStreak = 1;
        }

        // Record metric if linked and value provided
        bool metricRecorded = false;
        if (!string.IsNullOrEmpty(task.LinkedMetricCode) && request.MetricValue.HasValue)
        {
            var metricRecord = new MetricRecord
            {
                UserId = request.UserId,
                MetricCode = task.LinkedMetricCode,
                ValueNumber = request.MetricValue,
                RecordedAt = completedAt,
                Source = "task_completion"
            };
            _context.MetricRecords.Add(metricRecord);
            metricRecorded = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new TaskCompletionResponse
        {
            Data = new TaskCompletionData
            {
                Id = Guid.NewGuid(),
                Type = "taskCompletion",
                Attributes = new TaskCompletionAttributes
                {
                    TaskId = task.Id,
                    CompletedAt = completedAt,
                    CurrentStreak = currentStreak,
                    LongestStreak = longestStreak,
                    MetricRecorded = metricRecorded
                }
            }
        };
    }
}
