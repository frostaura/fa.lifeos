namespace LifeOS.Application.Interfaces;

public interface IFxRateProvider
{
    Task<IReadOnlyList<FxRateResult>> GetRatesAsync(CancellationToken cancellationToken = default);
}

public record FxRateResult(string BaseCurrency, string QuoteCurrency, decimal Rate);
