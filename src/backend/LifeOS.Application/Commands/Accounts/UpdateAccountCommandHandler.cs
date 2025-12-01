using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LifeOS.Application.Commands.Accounts;

public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public UpdateAccountCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == request.UserId, cancellationToken);

        if (account == null)
            return false;

        if (!string.IsNullOrEmpty(request.Name))
            account.Name = request.Name;

        if (request.AccountType.HasValue)
            account.AccountType = request.AccountType.Value;

        if (!string.IsNullOrEmpty(request.Currency))
            account.Currency = request.Currency;

        if (request.CurrentBalance.HasValue)
        {
            account.CurrentBalance = request.CurrentBalance.Value;
            account.BalanceUpdatedAt = DateTime.UtcNow;
        }

        // Institution can be set to null explicitly, so always update it
        account.Institution = request.Institution;

        if (request.IsLiability.HasValue)
            account.IsLiability = request.IsLiability.Value;

        if (request.InterestRateAnnual.HasValue)
            account.InterestRateAnnual = request.InterestRateAnnual.Value;

        if (request.InterestCompounding.HasValue)
            account.InterestCompounding = request.InterestCompounding.Value;

        if (request.MonthlyFee.HasValue)
            account.MonthlyFee = request.MonthlyFee.Value;

        if (request.Metadata != null)
            account.Metadata = JsonSerializer.Serialize(request.Metadata);

        if (request.IsActive.HasValue)
            account.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
