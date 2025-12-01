using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Transactions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Transactions;

public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, TransactionDetailResponse?>
{
    private readonly ILifeOSDbContext _context;

    public GetTransactionByIdQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<TransactionDetailResponse?> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _context.Transactions
            .AsNoTracking()
            .Include(t => t.SourceAccount)
            .Include(t => t.TargetAccount)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId && t.UserId == request.UserId, cancellationToken);

        if (transaction == null)
            return null;

        return new TransactionDetailResponse
        {
            Data = new TransactionItemResponse
            {
                Id = transaction.Id,
                Type = "transaction",
                Attributes = new TransactionAttributes
                {
                    SourceAccountId = transaction.SourceAccountId,
                    SourceAccountName = transaction.SourceAccount?.Name,
                    TargetAccountId = transaction.TargetAccountId,
                    TargetAccountName = transaction.TargetAccount?.Name,
                    Currency = transaction.Currency,
                    Amount = transaction.Amount,
                    AmountHomeCurrency = transaction.AmountHomeCurrency,
                    FxRateUsed = transaction.FxRateUsed,
                    Category = transaction.Category.ToString().ToLowerInvariant(),
                    Subcategory = transaction.Subcategory,
                    Tags = transaction.Tags,
                    Description = transaction.Description,
                    Notes = transaction.Notes,
                    TransactionDate = transaction.TransactionDate,
                    RecordedAt = transaction.RecordedAt,
                    Source = transaction.Source,
                    IsReconciled = transaction.IsReconciled
                }
            }
        };
    }
}
