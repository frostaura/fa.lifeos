using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Tasks;

public class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, TaskDetailResponse?>
{
    private readonly ILifeOSDbContext _context;

    public GetTaskByIdQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<TaskDetailResponse?> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var task = await _context.Tasks
            .AsNoTracking()
            .Include(t => t.Dimension)
            .Include(t => t.Streaks.Where(s => s.IsActive))
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == request.UserId, cancellationToken);

        if (task == null)
            return null;

        var streak = task.Streaks.FirstOrDefault();

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
                    DimensionCode = task.Dimension?.Code,
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
                    CurrentStreak = streak?.CurrentStreakLength ?? 0,
                    LongestStreak = streak?.LongestStreakLength ?? 0,
                    CreatedAt = task.CreatedAt
                }
            }
        };
    }
}
