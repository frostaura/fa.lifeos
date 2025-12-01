using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.Interfaces;
using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Hourly job to refresh FX rates from CoinGecko
/// </summary>
public class FxRateRefreshJob
{
    private readonly ILifeOSDbContext _context;
    private readonly IFxRateProvider _fxRateProvider;
    private readonly ILogger<FxRateRefreshJob> _logger;

    public FxRateRefreshJob(
        ILifeOSDbContext context,
        IFxRateProvider fxRateProvider,
        ILogger<FxRateRefreshJob> logger)
    {
        _context = context;
        _fxRateProvider = fxRateProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting FX rate refresh job");

        try
        {
            var rates = await _fxRateProvider.GetRatesAsync(cancellationToken);
            
            if (!rates.Any())
            {
                _logger.LogWarning("No FX rates returned from provider");
                return;
            }

            var now = DateTime.UtcNow;

            foreach (var rate in rates)
            {
                // Check if rate already exists
                var existing = await _context.FxRates
                    .FirstOrDefaultAsync(f => 
                        f.BaseCurrency == rate.BaseCurrency && 
                        f.QuoteCurrency == rate.QuoteCurrency, 
                        cancellationToken);

                if (existing != null)
                {
                    existing.Rate = rate.Rate;
                    existing.RateTimestamp = now;
                    existing.UpdatedAt = now;
                }
                else
                {
                    _context.FxRates.Add(new FxRate
                    {
                        BaseCurrency = rate.BaseCurrency,
                        QuoteCurrency = rate.QuoteCurrency,
                        Rate = rate.Rate,
                        RateTimestamp = now,
                        Source = "coingecko"
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("FX rate refresh completed. Updated {Count} rates", rates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FX rate refresh job failed");
            throw;
        }
    }
}
