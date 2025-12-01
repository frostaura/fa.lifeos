using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Common;
using LifeOS.Application.DTOs.Transactions;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Transactions;

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, TransactionListResponse>
{
    private readonly ILifeOSDbContext _context;

    public GetTransactionsQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<TransactionListResponse> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Transactions
            .AsNoTracking()
            .Include(t => t.SourceAccount)
            .Include(t => t.TargetAccount)
            .Where(t => t.UserId == request.UserId);

        if (request.AccountId.HasValue)
            query = query.Where(t => t.SourceAccountId == request.AccountId || t.TargetAccountId == request.AccountId);

        if (!string.IsNullOrEmpty(request.Category) &&
            Enum.TryParse<TransactionCategory>(request.Category, true, out var category))
            query = query.Where(t => t.Category == category);

        if (request.From.HasValue)
            query = query.Where(t => t.TransactionDate >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(t => t.TransactionDate <= request.To.Value);

        if (request.MinAmount.HasValue)
            query = query.Where(t => t.Amount >= request.MinAmount.Value);

        if (request.MaxAmount.HasValue)
            query = query.Where(t => t.Amount <= request.MaxAmount.Value);

        if (request.IsReconciled.HasValue)
            query = query.Where(t => t.IsReconciled == request.IsReconciled.Value);

        // Apply sorting
        query = request.Sort?.ToLowerInvariant() switch
        {
            "-transactiondate" => query.OrderByDescending(t => t.TransactionDate),
            "transactiondate" => query.OrderBy(t => t.TransactionDate),
            "-amount" => query.OrderByDescending(t => t.Amount),
            "amount" => query.OrderBy(t => t.Amount),
            _ => query.OrderByDescending(t => t.TransactionDate)
        };

        var total = await query.CountAsync(cancellationToken);
        var transactions = await query
            .Skip((request.Page - 1) * request.PerPage)
            .Take(request.PerPage)
            .ToListAsync(cancellationToken);

        return new TransactionListResponse
        {
            Data = transactions.Select(t => new TransactionItemResponse
            {
                Id = t.Id,
                Type = "transaction",
                Attributes = new TransactionAttributes
                {
                    SourceAccountId = t.SourceAccountId,
                    SourceAccountName = t.SourceAccount?.Name,
                    TargetAccountId = t.TargetAccountId,
                    TargetAccountName = t.TargetAccount?.Name,
                    Currency = t.Currency,
                    Amount = t.Amount,
                    AmountHomeCurrency = t.AmountHomeCurrency,
                    FxRateUsed = t.FxRateUsed,
                    Category = t.Category.ToString().ToLowerInvariant(),
                    Subcategory = t.Subcategory,
                    Tags = t.Tags,
                    Description = t.Description,
                    Notes = t.Notes,
                    TransactionDate = t.TransactionDate,
                    RecordedAt = t.RecordedAt,
                    Source = t.Source,
                    IsReconciled = t.IsReconciled
                }
            }).ToList(),
            Meta = new PaginationMeta
            {
                Page = request.Page,
                PerPage = request.PerPage,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)request.PerPage),
                Timestamp = DateTime.UtcNow
            }
        };
    }
}
