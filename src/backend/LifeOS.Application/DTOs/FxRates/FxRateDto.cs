namespace LifeOS.Application.DTOs.FxRates;

public record FxRateDto
{
    public string BaseCurrency { get; init; } = string.Empty;
    public string QuoteCurrency { get; init; } = string.Empty;
    public decimal Rate { get; init; }
    public DateTime RateTimestamp { get; init; }
    public string Source { get; init; } = "coingecko";
}

public record FxRateListResponse
{
    public List<FxRateDto> Data { get; init; } = new();
    public FxRateMeta? Meta { get; init; }
}

public record FxRateMeta
{
    public string HomeCurrency { get; init; } = "ZAR";
    public DateTime LastRefresh { get; init; }
    public DateTime NextScheduledRefresh { get; init; }
}

public record FxRateRefreshResponse
{
    public FxRateRefreshData Data { get; init; } = new();
}

public record FxRateRefreshData
{
    public int Refreshed { get; init; }
    public List<string> Pairs { get; init; } = new();
    public DateTime Timestamp { get; init; }
}
