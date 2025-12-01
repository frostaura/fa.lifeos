using MediatR;

namespace LifeOS.Application.Commands.Milestones;

public record DeleteMilestoneCommand(Guid UserId, Guid Id) : IRequest<bool>;
