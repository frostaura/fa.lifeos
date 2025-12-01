using LifeOS.Application.DTOs.Simulations;
using LifeOS.Application.Services;
using MediatR;

namespace LifeOS.Application.Commands.Simulations;

public record RunSimulationCommand(
    Guid UserId,
    Guid ScenarioId,
    bool RecalculateFromStart = true
) : IRequest<RunSimulationResponse>;
