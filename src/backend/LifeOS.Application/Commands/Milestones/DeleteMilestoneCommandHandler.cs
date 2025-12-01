using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Milestones;

public class DeleteMilestoneCommandHandler : IRequestHandler<DeleteMilestoneCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public DeleteMilestoneCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteMilestoneCommand request, CancellationToken cancellationToken)
    {
        var milestone = await _context.Milestones
            .FirstOrDefaultAsync(m => m.Id == request.Id && m.UserId == request.UserId, cancellationToken);

        if (milestone == null)
            return false;

        // Set milestone_id to null for related tasks
        var relatedTasks = await _context.Tasks
            .Where(t => t.MilestoneId == request.Id)
            .ToListAsync(cancellationToken);

        foreach (var task in relatedTasks)
        {
            task.MilestoneId = null;
        }

        _context.Milestones.Remove(milestone);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
