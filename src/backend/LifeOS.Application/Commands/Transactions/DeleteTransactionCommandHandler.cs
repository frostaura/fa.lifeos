using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Transactions;

public class DeleteTransactionCommandHandler : IRequestHandler<DeleteTransactionCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public DeleteTransactionCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId && t.UserId == request.UserId, cancellationToken);

        if (transaction == null)
            return false;

        // Reverse the balance changes
        if (transaction.SourceAccountId.HasValue)
        {
            var sourceAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == transaction.SourceAccountId, cancellationToken);
            if (sourceAccount != null)
            {
                sourceAccount.CurrentBalance += transaction.Amount;
                sourceAccount.BalanceUpdatedAt = DateTime.UtcNow;
            }
        }

        if (transaction.TargetAccountId.HasValue)
        {
            var targetAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == transaction.TargetAccountId, cancellationToken);
            if (targetAccount != null)
            {
                targetAccount.CurrentBalance -= transaction.Amount;
                targetAccount.BalanceUpdatedAt = DateTime.UtcNow;
            }
        }

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
