using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.FxRates;
using LifeOS.Application.Interfaces;
using LifeOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Finances;

public class RefreshFxRatesCommandHandler : IRequestHandler<RefreshFxRatesCommand, FxRateRefreshResponse>
{
    private readonly ILifeOSDbContext _context;
    private readonly IFxRateProvider _fxRateProvider;

    public RefreshFxRatesCommandHandler(ILifeOSDbContext context, IFxRateProvider fxRateProvider)
    {
        _context = context;
        _fxRateProvider = fxRateProvider;
    }

    public async Task<FxRateRefreshResponse> Handle(RefreshFxRatesCommand request, CancellationToken cancellationToken)
    {
        var rates = await _fxRateProvider.GetRatesAsync(cancellationToken);
        var timestamp = DateTime.UtcNow;
        var pairs = new List<string>();

        foreach (var rate in rates)
        {
            var fxRate = new FxRate
            {
                BaseCurrency = rate.BaseCurrency,
                QuoteCurrency = rate.QuoteCurrency,
                Rate = rate.Rate,
                RateTimestamp = timestamp,
                Source = "coingecko"
            };

            _context.FxRates.Add(fxRate);
            pairs.Add($"{rate.BaseCurrency}/{rate.QuoteCurrency}");
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new FxRateRefreshResponse
        {
            Data = new FxRateRefreshData
            {
                Refreshed = rates.Count,
                Pairs = pairs,
                Timestamp = timestamp
            }
        };
    }
}
