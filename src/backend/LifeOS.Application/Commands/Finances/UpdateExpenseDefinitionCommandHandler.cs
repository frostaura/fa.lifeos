using LifeOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Commands.Finances;

public class UpdateExpenseDefinitionCommandHandler : IRequestHandler<UpdateExpenseDefinitionCommand, bool>
{
    private readonly ILifeOSDbContext _context;

    public UpdateExpenseDefinitionCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateExpenseDefinitionCommand request, CancellationToken cancellationToken)
    {
        var expense = await _context.ExpenseDefinitions
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseDefinitionId && e.UserId == request.UserId, cancellationToken);

        if (expense == null)
            return false;

        if (!string.IsNullOrEmpty(request.Name))
            expense.Name = request.Name;

        if (request.AmountValue.HasValue)
            expense.AmountValue = request.AmountValue.Value;

        if (request.AmountFormula != null)
            expense.AmountFormula = request.AmountFormula;

        if (request.Frequency.HasValue)
            expense.Frequency = request.Frequency.Value;

        if (!string.IsNullOrEmpty(request.Category))
            expense.Category = request.Category;

        if (request.IsTaxDeductible.HasValue)
            expense.IsTaxDeductible = request.IsTaxDeductible.Value;

        if (request.LinkedAccountId.HasValue)
            expense.LinkedAccountId = request.LinkedAccountId.Value;

        if (request.InflationAdjusted.HasValue)
            expense.InflationAdjusted = request.InflationAdjusted.Value;

        if (request.IsActive.HasValue)
            expense.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
