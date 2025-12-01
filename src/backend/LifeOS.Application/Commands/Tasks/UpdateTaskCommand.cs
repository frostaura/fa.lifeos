using MediatR;

namespace LifeOS.Application.Commands.Tasks;

public record UpdateTaskCommand(
    Guid UserId,
    Guid Id,
    string? Title,
    string? Description,
    string? Frequency,
    DateOnly? ScheduledDate,
    TimeOnly? ScheduledTime,
    DateOnly? EndDate,
    bool? IsActive,
    string[]? Tags
) : IRequest<bool>;
