using LifeOS.Application.Common.Interfaces;
using LifeOS.Domain.Enums;
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

        if (request.StartDate.HasValue)
            expense.StartDate = request.StartDate.Value;

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

        // Handle end condition updates
        if (request.EndConditionType.HasValue)
            expense.EndConditionType = request.EndConditionType.Value;

        // Handle EndConditionAccountId - set if provided, clear if explicit flag or if switching to a type that doesn't need it
        if (request.EndConditionAccountId.HasValue)
            expense.EndConditionAccountId = request.EndConditionAccountId.Value;
        else if (request.ClearEndConditionAccount || 
                 (request.EndConditionType.HasValue && request.EndConditionType.Value != EndConditionType.UntilAccountSettled))
            expense.EndConditionAccountId = null;

        // Handle EndDate - set if provided, clear if switching to a type that doesn't need it
        if (request.EndDate.HasValue)
            expense.EndDate = request.EndDate.Value;
        else if (request.EndConditionType.HasValue && request.EndConditionType.Value != EndConditionType.UntilDate)
            expense.EndDate = null;

        // Handle EndAmountThreshold - set if provided, clear if switching to a type that doesn't need it
        if (request.EndAmountThreshold.HasValue)
            expense.EndAmountThreshold = request.EndAmountThreshold.Value;
        else if (request.EndConditionType.HasValue && request.EndConditionType.Value != EndConditionType.UntilAmount)
            expense.EndAmountThreshold = null;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
