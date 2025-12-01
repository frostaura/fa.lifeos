using LifeOS.Application.DTOs.Milestones;
using MediatR;

namespace LifeOS.Application.Commands.Milestones;

public record CreateMilestoneCommand(
    Guid UserId,
    string Title,
    string? Description,
    Guid DimensionId,
    DateOnly? TargetDate,
    string? TargetMetricCode,
    decimal? TargetMetricValue
) : IRequest<MilestoneDetailResponse>;
