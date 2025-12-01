using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Tasks;

public class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, TaskListResponse>
{
    private readonly ILifeOSDbContext _context;

    public GetTasksQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<TaskListResponse> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Tasks
            .AsNoTracking()
            .Include(t => t.Dimension)
            .Include(t => t.Streaks)
            .Where(t => t.UserId == request.UserId);

        if (!string.IsNullOrEmpty(request.TaskType) && Enum.TryParse<LifeOS.Domain.Enums.TaskType>(request.TaskType, true, out var taskType))
            query = query.Where(t => t.TaskType == taskType);

        if (request.DimensionId.HasValue)
            query = query.Where(t => t.DimensionId == request.DimensionId.Value);

        if (request.MilestoneId.HasValue)
            query = query.Where(t => t.MilestoneId == request.MilestoneId.Value);

        if (request.IsCompleted.HasValue)
            query = query.Where(t => t.IsCompleted == request.IsCompleted.Value);

        if (request.IsActive.HasValue)
            query = query.Where(t => t.IsActive == request.IsActive.Value);

        if (request.ScheduledFrom.HasValue)
            query = query.Where(t => t.ScheduledDate >= request.ScheduledFrom.Value);

        if (request.ScheduledTo.HasValue)
            query = query.Where(t => t.ScheduledDate <= request.ScheduledTo.Value);

        if (request.Tags?.Length > 0)
            query = query.Where(t => t.Tags != null && request.Tags.Any(tag => t.Tags.Contains(tag)));

        var total = await query.CountAsync(cancellationToken);
        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PerPage)
            .Take(request.PerPage)
            .ToListAsync(cancellationToken);

        return new TaskListResponse
        {
            Data = tasks.Select(t => 
            {
                var streak = t.Streaks.FirstOrDefault(s => s.IsActive);
                return new TaskItemResponse
                {
                    Id = t.Id,
                    Type = "task",
                    Attributes = new TaskAttributes
                    {
                        Title = t.Title,
                        Description = t.Description,
                        TaskType = t.TaskType.ToString().ToLowerInvariant(),
                        Frequency = t.Frequency.ToString().ToLowerInvariant(),
                        DimensionId = t.DimensionId,
                        DimensionCode = t.Dimension?.Code,
                        MilestoneId = t.MilestoneId,
                        LinkedMetricCode = t.LinkedMetricCode,
                        ScheduledDate = t.ScheduledDate,
                        ScheduledTime = t.ScheduledTime,
                        StartDate = t.StartDate,
                        EndDate = t.EndDate,
                        IsCompleted = t.IsCompleted,
                        CompletedAt = t.CompletedAt,
                        IsActive = t.IsActive,
                        Tags = t.Tags,
                        CurrentStreak = streak?.CurrentStreakLength ?? 0,
                        LongestStreak = streak?.LongestStreakLength ?? 0,
                        CreatedAt = t.CreatedAt
                    }
                };
            }).ToList(),
            Meta = new PaginationMeta
            {
                Page = request.Page,
                PerPage = request.PerPage,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)request.PerPage),
                Timestamp = DateTime.UtcNow
            }
        };
    }
}
