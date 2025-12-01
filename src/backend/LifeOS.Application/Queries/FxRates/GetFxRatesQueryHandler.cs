using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.FxRates;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.FxRates;

public class GetFxRatesQueryHandler : IRequestHandler<GetFxRatesQuery, FxRateListResponse>
{
    private readonly ILifeOSDbContext _context;

    public GetFxRatesQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<FxRateListResponse> Handle(GetFxRatesQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        var homeCurrency = user?.HomeCurrency ?? "ZAR";

        // Get the latest rate for each unique currency pair
        var latestRates = await _context.FxRates
            .AsNoTracking()
            .GroupBy(r => new { r.BaseCurrency, r.QuoteCurrency })
            .Select(g => g.OrderByDescending(r => r.RateTimestamp).First())
            .ToListAsync(cancellationToken);

        var lastRefresh = latestRates.Any() 
            ? latestRates.Max(r => r.RateTimestamp) 
            : DateTime.UtcNow;

        return new FxRateListResponse
        {
            Data = latestRates.Select(r => new FxRateDto
            {
                BaseCurrency = r.BaseCurrency,
                QuoteCurrency = r.QuoteCurrency,
                Rate = r.Rate,
                RateTimestamp = r.RateTimestamp,
                Source = r.Source
            }).ToList(),
            Meta = new FxRateMeta
            {
                HomeCurrency = homeCurrency,
                LastRefresh = lastRefresh,
                NextScheduledRefresh = lastRefresh.AddHours(1)
            }
        };
    }
}
