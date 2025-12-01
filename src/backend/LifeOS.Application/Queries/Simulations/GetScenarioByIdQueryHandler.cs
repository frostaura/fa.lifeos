using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Simulations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LifeOS.Application.Queries.Simulations;

public class GetScenarioByIdQueryHandler : IRequestHandler<GetScenarioByIdQuery, ScenarioDetailResponse?>
{
    private readonly ILifeOSDbContext _context;

    public GetScenarioByIdQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<ScenarioDetailResponse?> Handle(GetScenarioByIdQuery request, CancellationToken cancellationToken)
    {
        var scenario = await _context.SimulationScenarios
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ScenarioId && s.UserId == request.UserId, cancellationToken);

        if (scenario == null)
            return null;

        return new ScenarioDetailResponse
        {
            Data = new ScenarioItemResponse
            {
                Id = scenario.Id,
                Type = "simulationScenario",
                Attributes = new ScenarioAttributes
                {
                    Name = scenario.Name,
                    Description = scenario.Description,
                    StartDate = scenario.StartDate,
                    EndDate = scenario.EndDate,
                    EndCondition = scenario.EndCondition,
                    BaseAssumptions = TryParseAssumptions(scenario.BaseAssumptions),
                    IsBaseline = scenario.IsBaseline,
                    LastRunAt = scenario.LastRunAt
                }
            },
            Meta = new SimulationMeta { Timestamp = DateTime.UtcNow }
        };
    }

    private Dictionary<string, object>? TryParseAssumptions(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch
        {
            return null;
        }
    }
}
