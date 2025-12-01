using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Transactions;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Transactions;

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, TransactionDetailResponse>
{
    private readonly ILifeOSDbContext _context;

    public CreateTransactionCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<TransactionDetailResponse> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        var homeCurrency = user?.HomeCurrency ?? "ZAR";

        // Get FX rate if needed
        decimal? fxRate = null;
        decimal? amountHomeCurrency = request.Amount;

        if (request.Currency != homeCurrency)
        {
            var rate = await _context.FxRates
                .AsNoTracking()
                .Where(r => r.BaseCurrency == request.Currency && r.QuoteCurrency == homeCurrency)
                .OrderByDescending(r => r.RateTimestamp)
                .FirstOrDefaultAsync(cancellationToken);

            if (rate != null)
            {
                fxRate = rate.Rate;
                amountHomeCurrency = request.Amount * rate.Rate;
            }
        }

        var transaction = new Transaction
        {
            UserId = request.UserId,
            SourceAccountId = request.SourceAccountId,
            TargetAccountId = request.TargetAccountId,
            Currency = request.Currency.ToUpperInvariant(),
            Amount = request.Amount,
            AmountHomeCurrency = amountHomeCurrency,
            FxRateUsed = fxRate,
            Category = request.Category,
            Subcategory = request.Subcategory,
            Tags = request.Tags,
            Description = request.Description,
            Notes = request.Notes,
            TransactionDate = request.TransactionDate,
            RecordedAt = DateTime.UtcNow,
            Source = "manual",
            IsReconciled = false
        };

        _context.Transactions.Add(transaction);

        // Update account balances
        if (request.SourceAccountId.HasValue)
        {
            var sourceAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.SourceAccountId && a.UserId == request.UserId, cancellationToken);
            if (sourceAccount != null)
            {
                sourceAccount.CurrentBalance -= request.Amount;
                sourceAccount.BalanceUpdatedAt = DateTime.UtcNow;
            }
        }

        if (request.TargetAccountId.HasValue)
        {
            var targetAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.TargetAccountId && a.UserId == request.UserId, cancellationToken);
            if (targetAccount != null)
            {
                targetAccount.CurrentBalance += request.Amount;
                targetAccount.BalanceUpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Reload with navigation properties
        var createdTransaction = await _context.Transactions
            .AsNoTracking()
            .Include(t => t.SourceAccount)
            .Include(t => t.TargetAccount)
            .FirstAsync(t => t.Id == transaction.Id, cancellationToken);

        return new TransactionDetailResponse
        {
            Data = new TransactionItemResponse
            {
                Id = createdTransaction.Id,
                Type = "transaction",
                Attributes = new TransactionAttributes
                {
                    SourceAccountId = createdTransaction.SourceAccountId,
                    SourceAccountName = createdTransaction.SourceAccount?.Name,
                    TargetAccountId = createdTransaction.TargetAccountId,
                    TargetAccountName = createdTransaction.TargetAccount?.Name,
                    Currency = createdTransaction.Currency,
                    Amount = createdTransaction.Amount,
                    AmountHomeCurrency = createdTransaction.AmountHomeCurrency,
                    FxRateUsed = createdTransaction.FxRateUsed,
                    Category = createdTransaction.Category.ToString().ToLowerInvariant(),
                    Subcategory = createdTransaction.Subcategory,
                    Tags = createdTransaction.Tags,
                    Description = createdTransaction.Description,
                    Notes = createdTransaction.Notes,
                    TransactionDate = createdTransaction.TransactionDate,
                    RecordedAt = createdTransaction.RecordedAt,
                    Source = createdTransaction.Source,
                    IsReconciled = createdTransaction.IsReconciled
                }
            }
        };
    }
}
