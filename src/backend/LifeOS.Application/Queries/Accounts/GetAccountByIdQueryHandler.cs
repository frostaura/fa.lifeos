using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Accounts;
using LifeOS.Application.DTOs.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LifeOS.Application.Queries.Accounts;

public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, AccountDetailResponse?>
{
    private readonly ILifeOSDbContext _context;

    public GetAccountByIdQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<AccountDetailResponse?> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == request.UserId, cancellationToken);

        if (account == null)
            return null;

        return new AccountDetailResponse
        {
            Data = new AccountItemResponse
            {
                Id = account.Id,
                Type = "account",
                Attributes = new AccountAttributes
                {
                    Name = account.Name,
                    AccountType = account.AccountType.ToString().ToLowerInvariant(),
                    Currency = account.Currency,
                    InitialBalance = account.InitialBalance,
                    CurrentBalance = account.CurrentBalance,
                    CurrentBalanceHomeCurrency = account.CurrentBalance,
                    BalanceUpdatedAt = account.BalanceUpdatedAt,
                    Institution = account.Institution,
                    IsLiability = account.IsLiability,
                    InterestRateAnnual = account.InterestRateAnnual,
                    InterestCompounding = account.InterestCompounding?.ToString().ToLowerInvariant(),
                    MonthlyFee = account.MonthlyFee,
                    Metadata = ParseMetadata(account.Metadata),
                    IsActive = account.IsActive
                }
            },
            Meta = new ApiMeta { Timestamp = DateTime.UtcNow }
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
