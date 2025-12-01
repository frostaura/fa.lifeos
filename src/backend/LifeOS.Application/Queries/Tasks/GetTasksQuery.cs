using LifeOS.Application.DTOs.Tasks;
using MediatR;

namespace LifeOS.Application.Queries.Tasks;

public record GetTasksQuery(
    Guid UserId,
    string? TaskType = null,
    Guid? DimensionId = null,
    Guid? MilestoneId = null,
    bool? IsCompleted = null,
    bool? IsActive = null,
    DateOnly? ScheduledFrom = null,
    DateOnly? ScheduledTo = null,
    string[]? Tags = null,
    int Page = 1,
    int PerPage = 20
) : IRequest<TaskListResponse>;
