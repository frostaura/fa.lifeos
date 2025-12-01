using MediatR;

namespace LifeOS.Application.Commands.Tasks;

public record DeleteTaskCommand(Guid UserId, Guid Id) : IRequest<bool>;
