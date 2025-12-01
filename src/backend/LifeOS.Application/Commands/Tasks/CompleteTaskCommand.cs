using LifeOS.Application.DTOs.Tasks;
using MediatR;

namespace LifeOS.Application.Commands.Tasks;

public record CompleteTaskCommand(
    Guid UserId,
    Guid TaskId,
    DateTime? CompletedAt,
    decimal? MetricValue
) : IRequest<TaskCompletionResponse?>;
