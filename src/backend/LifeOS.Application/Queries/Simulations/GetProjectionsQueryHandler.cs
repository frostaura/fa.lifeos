using LifeOS.Application.DTOs.Simulations;
using LifeOS.Application.Services;
using MediatR;

namespace LifeOS.Application.Queries.Simulations;

public class GetProjectionsQueryHandler : IRequestHandler<GetProjectionsQuery, ProjectionResponse>
{
    private readonly ISimulationEngine _simulationEngine;

    public GetProjectionsQueryHandler(ISimulationEngine simulationEngine)
    {
        _simulationEngine = simulationEngine;
    }

    public async Task<ProjectionResponse> Handle(GetProjectionsQuery request, CancellationToken cancellationToken)
    {
        var data = await _simulationEngine.GetProjectionsAsync(
            request.UserId,
            request.ScenarioId,
            request.From,
            request.To,
            request.Granularity ?? "monthly",
            request.AccountId,
            cancellationToken);

        return new ProjectionResponse
        {
            Data = data,
            Meta = new ProjectionMeta
            {
                ScenarioId = request.ScenarioId,
                Granularity = request.Granularity ?? "monthly",
                From = request.From,
                To = request.To,
                Timestamp = DateTime.UtcNow
            }
        };
    }
}
