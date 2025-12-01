using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Simulations;

public class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public DeleteEventCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        // Find event and verify it belongs to user via scenario
        var evt = await _context.SimulationEvents
            .Include(e => e.Scenario)
            .FirstOrDefaultAsync(e => e.Id == request.EventId && e.Scenario.UserId == request.UserId, cancellationToken);

        if (evt == null)
            return false;

        _context.SimulationEvents.Remove(evt);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
