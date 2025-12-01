using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Milestones;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Milestones;

public class GetMilestoneByIdQueryHandler : IRequestHandler<GetMilestoneByIdQuery, MilestoneDetailResponse?>
{
    private readonly ILifeOSDbContext _context;

    public GetMilestoneByIdQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<MilestoneDetailResponse?> Handle(GetMilestoneByIdQuery request, CancellationToken cancellationToken)
    {
        var milestone = await _context.Milestones
            .AsNoTracking()
            .Include(m => m.Dimension)
            .FirstOrDefaultAsync(m => m.Id == request.Id && m.UserId == request.UserId, cancellationToken);

        if (milestone == null)
            return null;

        return new MilestoneDetailResponse
        {
            Data = new MilestoneItemResponse
            {
                Id = milestone.Id,
                Type = "milestone",
                Attributes = new MilestoneAttributes
                {
                    Title = milestone.Title,
                    Description = milestone.Description,
                    DimensionId = milestone.DimensionId,
                    DimensionCode = milestone.Dimension?.Code,
                    TargetDate = milestone.TargetDate,
                    TargetMetricCode = milestone.TargetMetricCode,
                    TargetMetricValue = milestone.TargetMetricValue,
                    Status = milestone.Status.ToString().ToLowerInvariant(),
                    CompletedAt = milestone.CompletedAt,
                    CreatedAt = milestone.CreatedAt
                }
            }
        };
    }
}
