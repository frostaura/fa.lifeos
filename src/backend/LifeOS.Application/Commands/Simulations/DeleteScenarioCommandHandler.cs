using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Simulations;

public class DeleteScenarioCommandHandler : IRequestHandler<DeleteScenarioCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public DeleteScenarioCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteScenarioCommand request, CancellationToken cancellationToken)
    {
        var scenario = await _context.SimulationScenarios
            .Include(s => s.Events)
            .Include(s => s.AccountProjections)
            .Include(s => s.NetWorthProjections)
            .FirstOrDefaultAsync(s => s.Id == request.ScenarioId && s.UserId == request.UserId, cancellationToken);

        if (scenario == null)
            return false;

        // Delete related data
        _context.SimulationEvents.RemoveRange(scenario.Events);
        _context.AccountProjections.RemoveRange(scenario.AccountProjections);
        _context.NetWorthProjections.RemoveRange(scenario.NetWorthProjections);
        _context.SimulationScenarios.Remove(scenario);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
