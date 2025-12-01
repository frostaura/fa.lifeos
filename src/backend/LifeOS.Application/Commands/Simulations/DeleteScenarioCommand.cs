using MediatR;

namespace LifeOS.Application.Commands.Simulations;

public record DeleteScenarioCommand(
    Guid UserId,
    Guid ScenarioId
) : IRequest<bool>;
