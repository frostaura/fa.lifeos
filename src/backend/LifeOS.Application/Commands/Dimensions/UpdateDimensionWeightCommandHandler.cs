using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Dimensions;

public class UpdateDimensionWeightCommandHandler : IRequestHandler<UpdateDimensionWeightCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public UpdateDimensionWeightCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateDimensionWeightCommand request, CancellationToken cancellationToken)
    {
        var dimension = await _context.Dimensions
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (dimension == null)
            return false;

        var oldWeight = dimension.DefaultWeight;
        var newWeight = request.Weight;
        dimension.DefaultWeight = newWeight;

        if (request.AutoRebalance)
        {
            var otherDimensions = await _context.Dimensions
                .Where(d => d.Id != request.Id && d.IsActive)
                .ToListAsync(cancellationToken);

            if (otherDimensions.Count > 0)
            {
                var remainingWeight = 1.0m - newWeight;
                var totalOtherWeight = otherDimensions.Sum(d => d.DefaultWeight);

                if (totalOtherWeight > 0)
                {
                    foreach (var other in otherDimensions)
                    {
                        other.DefaultWeight = (other.DefaultWeight / totalOtherWeight) * remainingWeight;
                    }
                }
                else
                {
                    var equalWeight = remainingWeight / otherDimensions.Count;
                    foreach (var other in otherDimensions)
                    {
                        other.DefaultWeight = equalWeight;
                    }
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
