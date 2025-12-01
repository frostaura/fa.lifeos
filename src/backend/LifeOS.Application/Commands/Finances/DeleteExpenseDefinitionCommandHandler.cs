using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Finances;

public class DeleteExpenseDefinitionCommandHandler : IRequestHandler<DeleteExpenseDefinitionCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public DeleteExpenseDefinitionCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteExpenseDefinitionCommand request, CancellationToken cancellationToken)
    {
        var expense = await _context.ExpenseDefinitions
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseDefinitionId && e.UserId == request.UserId, cancellationToken);

        if (expense == null)
            return false;

        _context.ExpenseDefinitions.Remove(expense);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
