using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Dimensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Dimensions;

public class GetDimensionsQueryHandler : IRequestHandler<GetDimensionsQuery, DimensionListResponse>
{
    private readonly ILifeOSDbContext _context;

    public GetDimensionsQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<DimensionListResponse> Handle(GetDimensionsQuery request, CancellationToken cancellationToken)
    {
        var dimensions = await _context.Dimensions
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ToListAsync(cancellationToken);

        var totalWeight = dimensions.Sum(d => d.DefaultWeight);

        return new DimensionListResponse
        {
            Data = dimensions.Select(d => new DimensionItemResponse
            {
                Id = d.Id,
                Type = "dimension",
                Attributes = new DimensionAttributes
                {
                    Code = d.Code,
                    Name = d.Name,
                    Description = d.Description,
                    Icon = d.Icon,
                    Weight = d.DefaultWeight,
                    DefaultWeight = d.DefaultWeight,
                    SortOrder = d.SortOrder,
                    IsActive = d.IsActive,
                    CurrentScore = 0 // Mock score for now
                }
            }).ToList(),
            Meta = new DimensionListMeta
            {
                TotalWeight = totalWeight
            }
        };
    }
}
