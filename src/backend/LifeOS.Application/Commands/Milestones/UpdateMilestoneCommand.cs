using MediatR;

namespace LifeOS.Application.Commands.Milestones;

public record UpdateMilestoneCommand(
    Guid UserId,
    Guid Id,
    string? Title,
    string? Description,
    DateOnly? TargetDate,
    string? Status
) : IRequest<bool>;
