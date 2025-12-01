using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Simulations;
using LifeOS.Domain.Entities;
using MediatR;
using System.Text.Json;

namespace LifeOS.Application.Commands.Simulations;

public class CreateScenarioCommandHandler : IRequestHandler<CreateScenarioCommand, ScenarioDetailResponse>
{
    private readonly ILifeOSDbContext _context;

    public CreateScenarioCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<ScenarioDetailResponse> Handle(CreateScenarioCommand request, CancellationToken cancellationToken)
    {
        var scenario = new SimulationScenario
        {
            UserId = request.UserId,
            Name = request.Name,
            Description = request.Description,
            StartDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = request.EndDate,
            EndCondition = request.EndCondition,
            BaseAssumptions = request.BaseAssumptions != null
                ? JsonSerializer.Serialize(request.BaseAssumptions)
                : "{}",
            IsBaseline = request.IsBaseline
        };

        _context.SimulationScenarios.Add(scenario);
        await _context.SaveChangesAsync(cancellationToken);

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
                    BaseAssumptions = request.BaseAssumptions,
                    IsBaseline = scenario.IsBaseline,
                    LastRunAt = scenario.LastRunAt
                }
            },
            Meta = new SimulationMeta { Timestamp = DateTime.UtcNow }
        };
    }
}
