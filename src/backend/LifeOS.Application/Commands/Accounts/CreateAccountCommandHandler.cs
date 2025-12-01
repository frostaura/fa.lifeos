using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Accounts;
using LifeOS.Application.DTOs.Common;
using LifeOS.Domain.Entities;
using MediatR;
using System.Text.Json;

namespace LifeOS.Application.Commands.Accounts;

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, AccountDetailResponse>
{
    private readonly ILifeOSDbContext _context;

    public CreateAccountCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<AccountDetailResponse> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = new Account
        {
            UserId = request.UserId,
            Name = request.Name,
            AccountType = request.AccountType,
            Currency = request.Currency.ToUpperInvariant(),
            InitialBalance = request.InitialBalance,
            CurrentBalance = request.InitialBalance,
            BalanceUpdatedAt = DateTime.UtcNow,
            Institution = request.Institution,
            IsLiability = request.IsLiability,
            InterestRateAnnual = request.InterestRateAnnual,
            InterestCompounding = request.InterestCompounding,
            MonthlyFee = request.MonthlyFee,
            Metadata = request.Metadata != null 
                ? JsonSerializer.Serialize(request.Metadata) 
                : "{}",
            IsActive = true
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);

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
                    Metadata = request.Metadata,
                    IsActive = account.IsActive
                }
            },
            Meta = new ApiMeta { Timestamp = DateTime.UtcNow }
        };
    }
}
