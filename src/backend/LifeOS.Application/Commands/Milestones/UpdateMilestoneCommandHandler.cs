using LifeOS.Application.Common.Interfaces;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Milestones;

public class UpdateMilestoneCommandHandler : IRequestHandler<UpdateMilestoneCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public UpdateMilestoneCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateMilestoneCommand request, CancellationToken cancellationToken)
    {
        var milestone = await _context.Milestones
            .FirstOrDefaultAsync(m => m.Id == request.Id && m.UserId == request.UserId, cancellationToken);

        if (milestone == null)
            return false;

        if (!string.IsNullOrEmpty(request.Title))
            milestone.Title = request.Title;

        if (request.Description != null)
            milestone.Description = request.Description;

        if (request.TargetDate.HasValue)
            milestone.TargetDate = request.TargetDate.Value;

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<MilestoneStatus>(request.Status, true, out var status))
        {
            milestone.Status = status;
            if (status == MilestoneStatus.Completed)
                milestone.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
