using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Dimensions;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Dimensions;

public class GetDimensionByIdQueryHandler : IRequestHandler<GetDimensionByIdQuery, DimensionDetailResponse?>
{
    private readonly ILifeOSDbContext _context;

    public GetDimensionByIdQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<DimensionDetailResponse?> Handle(GetDimensionByIdQuery request, CancellationToken cancellationToken)
    {
        var dimension = await _context.Dimensions
            .AsNoTracking()
            .Include(d => d.Milestones.Where(m => m.Status == MilestoneStatus.Active))
            .Include(d => d.Tasks.Where(t => t.IsActive))
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (dimension == null)
            return null;

        return new DimensionDetailResponse
        {
            Data = new DimensionDetailData
            {
                Id = dimension.Id,
                Type = "dimension",
                Attributes = new DimensionAttributes
                {
                    Code = dimension.Code,
                    Name = dimension.Name,
                    Description = dimension.Description,
                    Icon = dimension.Icon,
                    Weight = dimension.DefaultWeight,
                    DefaultWeight = dimension.DefaultWeight,
                    SortOrder = dimension.SortOrder,
                    IsActive = dimension.IsActive,
                    CurrentScore = 0
                },
                Relationships = new DimensionRelationships
                {
                    Milestones = dimension.Milestones.Select(m => new MilestoneReference
                    {
                        Id = m.Id,
                        Title = m.Title,
                        Status = m.Status.ToString().ToLowerInvariant()
                    }).ToList(),
                    ActiveTasks = dimension.Tasks.Select(t => new TaskReference
                    {
                        Id = t.Id,
                        Title = t.Title,
                        TaskType = t.TaskType.ToString().ToLowerInvariant()
                    }).ToList()
                }
            }
        };
    }
}
