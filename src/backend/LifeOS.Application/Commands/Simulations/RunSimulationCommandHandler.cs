using LifeOS.Application.DTOs.Simulations;
using LifeOS.Application.Services;
using MediatR;

namespace LifeOS.Application.Commands.Simulations;

public class RunSimulationCommandHandler : IRequestHandler<RunSimulationCommand, RunSimulationResponse>
{
    private readonly ISimulationEngine _simulationEngine;

    public RunSimulationCommandHandler(ISimulationEngine simulationEngine)
    {
        _simulationEngine = simulationEngine;
    }

    public async Task<RunSimulationResponse> Handle(RunSimulationCommand request, CancellationToken cancellationToken)
    {
        var result = await _simulationEngine.RunSimulationAsync(
            request.UserId,
            request.ScenarioId,
            request.RecalculateFromStart,
            cancellationToken);

        return new RunSimulationResponse
        {
            Data = result,
            Meta = new SimulationMeta { Timestamp = DateTime.UtcNow }
        };
    }
}
