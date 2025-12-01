using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Metrics;

public record DeleteMetricDefinitionCommand(string Code) : IRequest<bool>;

public class DeleteMetricDefinitionCommandHandler : IRequestHandler<DeleteMetricDefinitionCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public DeleteMetricDefinitionCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteMetricDefinitionCommand request, CancellationToken cancellationToken)
    {
        var definition = await _context.MetricDefinitions
            .FirstOrDefaultAsync(m => m.Code == request.Code, cancellationToken);

        if (definition == null)
            return false;

        // Soft delete - set IsActive to false
        definition.IsActive = false;
        definition.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
