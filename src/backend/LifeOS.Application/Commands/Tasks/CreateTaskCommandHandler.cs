using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Tasks;
using LifeOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Tasks;

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskDetailResponse>
{
    private readonly ILifeOSDbContext _context;

    public CreateTaskCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<TaskDetailResponse> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var dimension = request.DimensionId.HasValue 
            ? await _context.Dimensions.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.DimensionId.Value, cancellationToken)
            : null;

        var taskType = Enum.TryParse<LifeOS.Domain.Enums.TaskType>(request.TaskType, true, out var tt) 
            ? tt 
            : LifeOS.Domain.Enums.TaskType.OneOff;
            
        var frequency = Enum.TryParse<LifeOS.Domain.Enums.Frequency>(request.Frequency, true, out var freq) 
            ? freq 
            : LifeOS.Domain.Enums.Frequency.AdHoc;

        var task = new LifeTask
        {
            UserId = request.UserId,
            Title = request.Title,
            Description = request.Description,
            TaskType = taskType,
            Frequency = frequency,
            DimensionId = request.DimensionId,
            MilestoneId = request.MilestoneId,
            LinkedMetricCode = request.LinkedMetricCode,
            ScheduledDate = request.ScheduledDate,
            ScheduledTime = request.ScheduledTime,
            StartDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = request.EndDate,
            Tags = request.Tags,
            IsActive = true,
            IsCompleted = false
        };

        _context.Tasks.Add(task);

        // Create streak for habits
        if (taskType == LifeOS.Domain.Enums.TaskType.Habit)
        {
            var streak = new Streak
            {
                UserId = request.UserId,
                TaskId = task.Id,
                CurrentStreakLength = 0,
                LongestStreakLength = 0,
                IsActive = true
            };
            _context.Streaks.Add(streak);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new TaskDetailResponse
        {
            Data = new TaskItemResponse
            {
                Id = task.Id,
                Type = "task",
                Attributes = new TaskAttributes
                {
                    Title = task.Title,
                    Description = task.Description,
                    TaskType = task.TaskType.ToString().ToLowerInvariant(),
                    Frequency = task.Frequency.ToString().ToLowerInvariant(),
                    DimensionId = task.DimensionId,
                    DimensionCode = dimension?.Code,
                    MilestoneId = task.MilestoneId,
                    LinkedMetricCode = task.LinkedMetricCode,
                    ScheduledDate = task.ScheduledDate,
                    ScheduledTime = task.ScheduledTime,
                    StartDate = task.StartDate,
                    EndDate = task.EndDate,
                    IsCompleted = task.IsCompleted,
                    CompletedAt = task.CompletedAt,
                    IsActive = task.IsActive,
                    Tags = task.Tags,
                    CurrentStreak = 0,
                    LongestStreak = 0,
                    CreatedAt = task.CreatedAt
                }
            }
        };
    }
}
