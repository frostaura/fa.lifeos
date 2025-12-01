using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Simulations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LifeOS.Application.Queries.Simulations;

public class GetScenariosQueryHandler : IRequestHandler<GetScenariosQuery, ScenarioListResponse>
{
    private readonly ILifeOSDbContext _context;

    public GetScenariosQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<ScenarioListResponse> Handle(GetScenariosQuery request, CancellationToken cancellationToken)
    {
        var scenarios = await _context.SimulationScenarios
            .Where(s => s.UserId == request.UserId)
            .OrderByDescending(s => s.IsBaseline)
            .ThenByDescending(s => s.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new ScenarioListResponse
        {
            Data = scenarios.Select(s => new ScenarioItemResponse
            {
                Id = s.Id,
                Type = "simulationScenario",
                Attributes = new ScenarioAttributes
                {
                    Name = s.Name,
                    Description = s.Description,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    EndCondition = s.EndCondition,
                    BaseAssumptions = TryParseAssumptions(s.BaseAssumptions),
                    IsBaseline = s.IsBaseline,
                    LastRunAt = s.LastRunAt
                }
            }).ToList(),
            Meta = new ScenarioListMeta
            {
                Total = scenarios.Count,
                Timestamp = DateTime.UtcNow
            }
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
