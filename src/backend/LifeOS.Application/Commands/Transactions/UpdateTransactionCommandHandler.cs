using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Transactions;

public class UpdateTransactionCommandHandler : IRequestHandler<UpdateTransactionCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public UpdateTransactionCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId && t.UserId == request.UserId, cancellationToken);

        if (transaction == null)
            return false;

        if (request.Subcategory != null)
            transaction.Subcategory = request.Subcategory;

        if (request.Tags != null)
            transaction.Tags = request.Tags;

        if (request.Description != null)
            transaction.Description = request.Description;

        if (request.Notes != null)
            transaction.Notes = request.Notes;

        if (request.IsReconciled.HasValue)
            transaction.IsReconciled = request.IsReconciled.Value;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
