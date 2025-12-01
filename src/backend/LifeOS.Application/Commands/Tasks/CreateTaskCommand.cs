using LifeOS.Application.DTOs.Tasks;
using MediatR;

namespace LifeOS.Application.Commands.Tasks;

public record CreateTaskCommand(
    Guid UserId,
    string Title,
    string? Description,
    string TaskType,
    string Frequency,
    Guid? DimensionId,
    Guid? MilestoneId,
    string? LinkedMetricCode,
    DateOnly? ScheduledDate,
    TimeOnly? ScheduledTime,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string[]? Tags
) : IRequest<TaskDetailResponse>;
