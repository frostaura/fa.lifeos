using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Tasks;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public DeleteTaskCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.Tasks
            .Include(t => t.Streaks)
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == request.UserId, cancellationToken);

        if (task == null)
            return false;

        // Remove associated streaks
        foreach (var streak in task.Streaks)
        {
            _context.Streaks.Remove(streak);
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
