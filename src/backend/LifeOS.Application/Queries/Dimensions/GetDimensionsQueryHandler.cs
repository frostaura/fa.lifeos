using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Dimensions;
using LifeOS.Application.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Dimensions;

public class GetDimensionsQueryHandler : IRequestHandler<GetDimensionsQuery, DimensionListResponse>
{
    private readonly ILifeOSDbContext _context;
    private readonly IScoreCalculator _scoreCalculator;

    public GetDimensionsQueryHandler(ILifeOSDbContext context, IScoreCalculator scoreCalculator)
    {
        _context = context;
        _scoreCalculator = scoreCalculator;
    }

    public async Task<DimensionListResponse> Handle(GetDimensionsQuery request, CancellationToken cancellationToken)
    {
        var dimensions = await _context.Dimensions
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ToListAsync(cancellationToken);

        var totalWeight = dimensions.Sum(d => d.DefaultWeight);

        // Calculate scores for all dimensions
        var dimensionResponses = new List<DimensionItemResponse>();
        foreach (var d in dimensions)
        {
            var score = await _scoreCalculator.CalculateDimensionScoreAsync(request.UserId, d.Id, cancellationToken);
            dimensionResponses.Add(new DimensionItemResponse
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
                    CurrentScore = (int)Math.Round(score)
                }
            });
        }

        return new DimensionListResponse
        {
            Data = dimensionResponses,
            Meta = new DimensionListMeta
            {
                TotalWeight = totalWeight
            }
        };
    }
}
