using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Common;
using LifeOS.Application.DTOs.Milestones;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Milestones;

public class GetMilestonesQueryHandler : IRequestHandler<GetMilestonesQuery, MilestoneListResponse>
{
    private readonly ILifeOSDbContext _context;

    public GetMilestonesQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<MilestoneListResponse> Handle(GetMilestonesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Milestones
            .AsNoTracking()
            .Include(m => m.Dimension)
            .Where(m => m.UserId == request.UserId);

        if (request.DimensionId.HasValue)
            query = query.Where(m => m.DimensionId == request.DimensionId.Value);

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<MilestoneStatus>(request.Status, true, out var status))
            query = query.Where(m => m.Status == status);

        // Apply sorting
        query = request.Sort?.ToLowerInvariant() switch
        {
            "-targetdate" => query.OrderByDescending(m => m.TargetDate),
            "targetdate" => query.OrderBy(m => m.TargetDate),
            "title" => query.OrderBy(m => m.Title),
            "-title" => query.OrderByDescending(m => m.Title),
            _ => query.OrderByDescending(m => m.CreatedAt)
        };

        var total = await query.CountAsync(cancellationToken);
        var milestones = await query
            .Skip((request.Page - 1) * request.PerPage)
            .Take(request.PerPage)
            .ToListAsync(cancellationToken);

        return new MilestoneListResponse
        {
            Data = milestones.Select(m => new MilestoneItemResponse
            {
                Id = m.Id,
                Type = "milestone",
                Attributes = new MilestoneAttributes
                {
                    Title = m.Title,
                    Description = m.Description,
                    DimensionId = m.DimensionId,
                    DimensionCode = m.Dimension?.Code,
                    TargetDate = m.TargetDate,
                    TargetMetricCode = m.TargetMetricCode,
                    TargetMetricValue = m.TargetMetricValue,
                    Status = m.Status.ToString().ToLowerInvariant(),
                    CompletedAt = m.CompletedAt,
                    CreatedAt = m.CreatedAt
                }
            }).ToList(),
            Meta = new PaginationMeta
            {
                Page = request.Page,
                PerPage = request.PerPage,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)request.PerPage),
                Timestamp = DateTime.UtcNow
            }
        };
    }
}
