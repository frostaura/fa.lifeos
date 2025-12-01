using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Milestones;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Milestones;

public class CreateMilestoneCommandHandler : IRequestHandler<CreateMilestoneCommand, MilestoneDetailResponse>
{
    private readonly ILifeOSDbContext _context;

    public CreateMilestoneCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<MilestoneDetailResponse> Handle(CreateMilestoneCommand request, CancellationToken cancellationToken)
    {
        var dimension = await _context.Dimensions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DimensionId, cancellationToken);

        var milestone = new Milestone
        {
            UserId = request.UserId,
            DimensionId = request.DimensionId,
            Title = request.Title,
            Description = request.Description,
            TargetDate = request.TargetDate,
            TargetMetricCode = request.TargetMetricCode,
            TargetMetricValue = request.TargetMetricValue,
            Status = MilestoneStatus.Active
        };

        _context.Milestones.Add(milestone);
        await _context.SaveChangesAsync(cancellationToken);

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
                    DimensionCode = dimension?.Code,
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
