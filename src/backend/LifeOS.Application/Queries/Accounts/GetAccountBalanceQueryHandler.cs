using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Accounts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Accounts;

public class GetAccountBalanceQueryHandler : IRequestHandler<GetAccountBalanceQuery, AccountBalanceResponse?>
{
    private readonly ILifeOSDbContext _context;

    public GetAccountBalanceQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<AccountBalanceResponse?> Handle(GetAccountBalanceQuery request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == request.UserId, cancellationToken);

        if (account == null)
            return null;

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        var targetCurrency = request.TargetCurrency ?? user?.HomeCurrency ?? "ZAR";
        var originalCurrency = account.Currency;

        // Calculate balance from transactions
        var transactionBalance = await CalculateBalanceFromTransactions(account.Id, cancellationToken);
        var currentBalance = account.InitialBalance + transactionBalance;

        // Get FX rate if needed
        decimal fxRate = 1m;
        DateTime fxTimestamp = DateTime.UtcNow;
        string fxSource = "none";

        if (originalCurrency != targetCurrency)
        {
            var rate = await GetFxRate(originalCurrency, targetCurrency, cancellationToken);
            if (rate != null)
            {
                fxRate = rate.Rate;
                fxTimestamp = rate.RateTimestamp;
                fxSource = rate.Source;
            }
        }

        var convertedBalance = currentBalance * fxRate;

        return new AccountBalanceResponse
        {
            Data = new AccountBalanceData
            {
                AccountId = account.Id,
                OriginalCurrency = originalCurrency,
                OriginalBalance = currentBalance,
                TargetCurrency = targetCurrency,
                ConvertedBalance = convertedBalance,
                FxRate = fxRate,
                FxRateTimestamp = fxTimestamp,
                FxSource = fxSource
            }
        };
    }

    private async Task<decimal> CalculateBalanceFromTransactions(Guid accountId, CancellationToken cancellationToken)
    {
        var debits = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.SourceAccountId == accountId)
            .SumAsync(t => t.Amount, cancellationToken);

        var credits = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.TargetAccountId == accountId)
            .SumAsync(t => t.Amount, cancellationToken);

        return credits - debits;
    }

    private async Task<FxRateInfo?> GetFxRate(string baseCurrency, string quoteCurrency, CancellationToken cancellationToken)
    {
        // Try direct rate
        var rate = await _context.FxRates
            .AsNoTracking()
            .Where(r => r.BaseCurrency == baseCurrency && r.QuoteCurrency == quoteCurrency)
            .OrderByDescending(r => r.RateTimestamp)
            .FirstOrDefaultAsync(cancellationToken);

        if (rate != null)
            return new FxRateInfo(rate.Rate, rate.RateTimestamp, rate.Source);

        // Try inverse rate
        var inverseRate = await _context.FxRates
            .AsNoTracking()
            .Where(r => r.BaseCurrency == quoteCurrency && r.QuoteCurrency == baseCurrency)
            .OrderByDescending(r => r.RateTimestamp)
            .FirstOrDefaultAsync(cancellationToken);

        if (inverseRate != null)
            return new FxRateInfo(1m / inverseRate.Rate, inverseRate.RateTimestamp, inverseRate.Source);

        // Try via USD cross rate
        var baseToUsd = await _context.FxRates
            .AsNoTracking()
            .Where(r => r.BaseCurrency == baseCurrency && r.QuoteCurrency == "USD")
            .OrderByDescending(r => r.RateTimestamp)
            .FirstOrDefaultAsync(cancellationToken);

        var usdToQuote = await _context.FxRates
            .AsNoTracking()
            .Where(r => r.BaseCurrency == "USD" && r.QuoteCurrency == quoteCurrency)
            .OrderByDescending(r => r.RateTimestamp)
            .FirstOrDefaultAsync(cancellationToken);

        if (baseToUsd != null && usdToQuote != null)
            return new FxRateInfo(baseToUsd.Rate * usdToQuote.Rate, DateTime.UtcNow, "calculated");

        return null;
    }

    private record FxRateInfo(decimal Rate, DateTime RateTimestamp, string Source);
}
