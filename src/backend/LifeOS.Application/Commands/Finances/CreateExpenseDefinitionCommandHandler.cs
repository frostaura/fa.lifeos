using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Finances;
using LifeOS.Domain.Entities;
using MediatR;

namespace LifeOS.Application.Commands.Finances;

public class CreateExpenseDefinitionCommandHandler : IRequestHandler<CreateExpenseDefinitionCommand, ExpenseDefinitionDetailResponse>
{
    private readonly ILifeOSDbContext _context;

    public CreateExpenseDefinitionCommandHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<ExpenseDefinitionDetailResponse> Handle(CreateExpenseDefinitionCommand request, CancellationToken cancellationToken)
    {
        var expense = new ExpenseDefinition
        {
            UserId = request.UserId,
            Name = request.Name,
            Currency = request.Currency.ToUpperInvariant(),
            AmountType = request.AmountType,
            AmountValue = request.AmountValue,
            AmountFormula = request.AmountFormula,
            Frequency = request.Frequency,
            Category = request.Category,
            IsTaxDeductible = request.IsTaxDeductible,
            LinkedAccountId = request.LinkedAccountId,
            InflationAdjusted = request.InflationAdjusted,
            IsActive = true
        };

        _context.ExpenseDefinitions.Add(expense);
        await _context.SaveChangesAsync(cancellationToken);

        return new ExpenseDefinitionDetailResponse
        {
            Data = new ExpenseDefinitionItemResponse
            {
                Id = expense.Id,
                Type = "expenseDefinition",
                Attributes = new ExpenseDefinitionAttributes
                {
                    Name = expense.Name,
                    Currency = expense.Currency,
                    AmountType = expense.AmountType.ToString().ToLowerInvariant(),
                    AmountValue = expense.AmountValue,
                    AmountFormula = expense.AmountFormula,
                    Frequency = expense.Frequency.ToString().ToLowerInvariant(),
                    Category = expense.Category,
                    IsTaxDeductible = expense.IsTaxDeductible,
                    LinkedAccountId = expense.LinkedAccountId,
                    InflationAdjusted = expense.InflationAdjusted,
                    IsActive = expense.IsActive
                }
            }
        };
    }
}
