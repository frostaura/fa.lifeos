using MediatR;

namespace LifeOS.Application.Commands.Simulations;

public record DeleteEventCommand(
    Guid UserId,
    Guid EventId
) : IRequest<bool>;
