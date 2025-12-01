using System.Net.Http.Json;
using System.Text.Json;
using LifeOS.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LifeOS.Infrastructure.Services.FxRates;

public class CoinGeckoFxRateProvider : IFxRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CoinGeckoFxRateProvider> _logger;
    private const string CacheKey = "fx_rates_coingecko";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    // CoinGecko API base URL
    private const string BaseUrl = "https://api.coingecko.com/api/v3";

    // Currency mappings for CoinGecko
    private static readonly Dictionary<string, string> CryptoCurrencies = new()
    {
        { "BTC", "bitcoin" },
        { "ETH", "ethereum" }
    };

    private static readonly string[] SupportedFiatCurrencies = { "ZAR", "USD", "EUR", "GBP" };

    public CoinGeckoFxRateProvider(
        HttpClient httpClient, 
        IMemoryCache cache,
        ILogger<CoinGeckoFxRateProvider> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FxRateResult>> GetRatesAsync(CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<FxRateResult>? cachedRates) && cachedRates != null)
        {
            _logger.LogDebug("Returning cached FX rates");
            return cachedRates;
        }

        try
        {
            var rates = new List<FxRateResult>();

            // Fetch crypto prices (BTC, ETH) in all fiat currencies
            var cryptoRates = await FetchCryptoRatesAsync(cancellationToken);
            rates.AddRange(cryptoRates);

            // Fetch fiat exchange rates via USD as base
            var fiatRates = await FetchFiatRatesAsync(cancellationToken);
            rates.AddRange(fiatRates);

            // Cache the results
            _cache.Set(CacheKey, (IReadOnlyList<FxRateResult>)rates, CacheDuration);
            
            _logger.LogInformation("Fetched and cached {Count} FX rates from CoinGecko", rates.Count);
            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch FX rates from CoinGecko");
            
            // Return fallback rates if available in cache (even if expired)
            if (_cache.TryGetValue($"{CacheKey}_fallback", out IReadOnlyList<FxRateResult>? fallbackRates) && fallbackRates != null)
            {
                _logger.LogWarning("Using fallback FX rates");
                return fallbackRates;
            }

            // Return empty list if no fallback available
            return Array.Empty<FxRateResult>();
        }
    }

    private async Task<List<FxRateResult>> FetchCryptoRatesAsync(CancellationToken cancellationToken)
    {
        var rates = new List<FxRateResult>();
        
        try
        {
            var cryptoIds = string.Join(",", CryptoCurrencies.Values);
            var currencies = string.Join(",", SupportedFiatCurrencies.Select(c => c.ToLowerInvariant()));
            
            var url = $"{BaseUrl}/simple/price?ids={cryptoIds}&vs_currencies={currencies}";
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(content);
            
            foreach (var (cryptoSymbol, coinGeckoId) in CryptoCurrencies)
            {
                if (doc.RootElement.TryGetProperty(coinGeckoId, out var cryptoData))
                {
                    foreach (var fiat in SupportedFiatCurrencies)
                    {
                        var fiatLower = fiat.ToLowerInvariant();
                        if (cryptoData.TryGetProperty(fiatLower, out var priceElement))
                        {
                            var price = priceElement.GetDecimal();
                            rates.Add(new FxRateResult(cryptoSymbol, fiat, price));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch crypto rates");
        }
        
        return rates;
    }

    private async Task<List<FxRateResult>> FetchFiatRatesAsync(CancellationToken cancellationToken)
    {
        var rates = new List<FxRateResult>();
        
        try
        {
            // Use CoinGecko's exchange_rates endpoint to get fiat conversions
            var url = $"{BaseUrl}/exchange_rates";
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(content);
            
            if (doc.RootElement.TryGetProperty("rates", out var ratesData))
            {
                // Get USD rate as base (value of 1 BTC in USD)
                decimal usdRate = 1m;
                if (ratesData.TryGetProperty("usd", out var usdData) && 
                    usdData.TryGetProperty("value", out var usdValue))
                {
                    usdRate = usdValue.GetDecimal();
                }
                
                foreach (var fiat in SupportedFiatCurrencies.Where(f => f != "USD"))
                {
                    var fiatLower = fiat.ToLowerInvariant();
                    if (ratesData.TryGetProperty(fiatLower, out var fiatData) &&
                        fiatData.TryGetProperty("value", out var fiatValue))
                    {
                        var fiatRate = fiatValue.GetDecimal();
                        // Convert from BTC base to USD base: USD -> FIAT
                        var usdToFiat = fiatRate / usdRate;
                        rates.Add(new FxRateResult("USD", fiat, usdToFiat));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch fiat rates");
        }
        
        return rates;
    }
}
