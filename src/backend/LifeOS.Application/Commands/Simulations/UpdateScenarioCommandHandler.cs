using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LifeOS.Application.Commands.Simulations;

public class UpdateScenarioCommandHandler : IRequestHandler<UpdateScenarioCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public UpdateScenarioCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateScenarioCommand request, CancellationToken cancellationToken)
    {
        var scenario = await _context.SimulationScenarios
            .FirstOrDefaultAsync(s => s.Id == request.ScenarioId && s.UserId == request.UserId, cancellationToken);

        if (scenario == null)
            return false;

        if (request.Name != null)
            scenario.Name = request.Name;

        if (request.Description != null)
            scenario.Description = request.Description;

        if (request.EndDate.HasValue)
            scenario.EndDate = request.EndDate.Value;

        if (request.EndCondition != null)
            scenario.EndCondition = request.EndCondition;

        if (request.BaseAssumptions != null)
            scenario.BaseAssumptions = JsonSerializer.Serialize(request.BaseAssumptions);

        if (request.IsBaseline.HasValue)
            scenario.IsBaseline = request.IsBaseline.Value;

        scenario.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
