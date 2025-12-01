using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Tasks;

public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public UpdateTaskCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == request.UserId, cancellationToken);

        if (task == null)
            return false;

        if (!string.IsNullOrEmpty(request.Title))
            task.Title = request.Title;

        if (request.Description != null)
            task.Description = request.Description;

        if (!string.IsNullOrEmpty(request.Frequency) && Enum.TryParse<LifeOS.Domain.Enums.Frequency>(request.Frequency, true, out var freq))
            task.Frequency = freq;

        if (request.ScheduledDate.HasValue)
            task.ScheduledDate = request.ScheduledDate;

        if (request.ScheduledTime.HasValue)
            task.ScheduledTime = request.ScheduledTime;

        if (request.EndDate.HasValue)
            task.EndDate = request.EndDate;

        if (request.IsActive.HasValue)
            task.IsActive = request.IsActive.Value;

        if (request.Tags != null)
            task.Tags = request.Tags;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
