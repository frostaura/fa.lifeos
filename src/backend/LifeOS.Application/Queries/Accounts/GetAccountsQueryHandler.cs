using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Accounts;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LifeOS.Application.Queries.Accounts;

public class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, AccountListResponse>
{
    private readonly ILifeOSDbContext _context;

    public GetAccountsQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<AccountListResponse> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == request.UserId);

        if (!string.IsNullOrEmpty(request.AccountType) && 
            Enum.TryParse<AccountType>(request.AccountType, true, out var accountType))
            query = query.Where(a => a.AccountType == accountType);

        if (!string.IsNullOrEmpty(request.Currency))
            query = query.Where(a => a.Currency == request.Currency);

        if (request.IsActive.HasValue)
            query = query.Where(a => a.IsActive == request.IsActive.Value);

        if (request.IsLiability.HasValue)
            query = query.Where(a => a.IsLiability == request.IsLiability.Value);

        var total = await query.CountAsync(cancellationToken);
        var accounts = await query
            .OrderBy(a => a.Name)
            .Skip((request.Page - 1) * request.PerPage)
            .Take(request.PerPage)
            .ToListAsync(cancellationToken);

        // Calculate totals for all active accounts
        var allActiveAccounts = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == request.UserId && a.IsActive)
            .ToListAsync(cancellationToken);

        var totalAssets = allActiveAccounts.Where(a => !a.IsLiability).Sum(a => a.CurrentBalance);
        var totalLiabilities = allActiveAccounts.Where(a => a.IsLiability).Sum(a => Math.Abs(a.CurrentBalance));
        var netWorth = totalAssets - totalLiabilities;

        // Calculate total monthly interest for liability accounts (rate is stored as percentage, e.g., 21.75 = 21.75%)
        var totalMonthlyInterest = allActiveAccounts
            .Where(a => a.IsLiability && a.InterestRateAnnual.HasValue && a.InterestRateAnnual.Value > 0)
            .Sum(a => Math.Abs(a.CurrentBalance) * (a.InterestRateAnnual!.Value / 100m / 12m));

        // Calculate total monthly fees for all accounts
        var totalMonthlyFees = allActiveAccounts.Sum(a => a.MonthlyFee);

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        return new AccountListResponse
        {
            Data = accounts.Select(a => new AccountItemResponse
            {
                Id = a.Id,
                Type = "account",
                Attributes = new AccountAttributes
                {
                    Name = a.Name,
                    AccountType = a.AccountType.ToString().ToLowerInvariant(),
                    Currency = a.Currency,
                    InitialBalance = a.InitialBalance,
                    CurrentBalance = a.CurrentBalance,
                    CurrentBalanceHomeCurrency = a.CurrentBalance, // TODO: Apply FX conversion
                    BalanceUpdatedAt = a.BalanceUpdatedAt,
                    Institution = a.Institution,
                    IsLiability = a.IsLiability,
                    InterestRateAnnual = a.InterestRateAnnual,
                    InterestCompounding = a.InterestCompounding?.ToString().ToLowerInvariant(),
                    MonthlyInterest = a.IsLiability && a.InterestRateAnnual.HasValue && a.InterestRateAnnual.Value > 0
                        ? Math.Abs(a.CurrentBalance) * (a.InterestRateAnnual.Value / 100m / 12m)
                        : null,
                    MonthlyFee = a.MonthlyFee,
                    Metadata = ParseMetadata(a.Metadata),
                    IsActive = a.IsActive
                }
            }).ToList(),
            Meta = new AccountListMeta
            {
                Page = request.Page,
                PerPage = request.PerPage,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)request.PerPage),
                TotalAssets = totalAssets,
                TotalLiabilities = totalLiabilities,
                NetWorth = netWorth,
                TotalMonthlyInterest = totalMonthlyInterest,
                TotalMonthlyFees = totalMonthlyFees,
                HomeCurrency = user?.HomeCurrency ?? "ZAR",
                Timestamp = DateTime.UtcNow
            }
        };
    }

    private static Dictionary<string, object>? ParseMetadata(string? metadata)
    {
        if (string.IsNullOrEmpty(metadata) || metadata == "{}")
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(metadata);
        }
        catch
        {
            return null;
        }
    }
}
