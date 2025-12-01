using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Finances;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Finances;

public class GetExpenseDefinitionsQueryHandler : IRequestHandler<GetExpenseDefinitionsQuery, ExpenseDefinitionListResponse>
{
    private readonly ILifeOSDbContext _context;

    public GetExpenseDefinitionsQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<ExpenseDefinitionListResponse> Handle(GetExpenseDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var expenses = await _context.ExpenseDefinitions
            .AsNoTracking()
            .Where(e => e.UserId == request.UserId)
            .OrderBy(e => e.Category)
            .ThenBy(e => e.Name)
            .ToListAsync(cancellationToken);

        var activeExpenses = expenses.Where(e => e.IsActive).ToList();
        var totalMonthly = activeExpenses.Sum(e => ConvertToMonthly(e.AmountValue ?? 0, e.Frequency));
        
        var byCategory = activeExpenses
            .GroupBy(e => e.Category)
            .ToDictionary(
                g => g.Key, 
                g => g.Sum(e => ConvertToMonthly(e.AmountValue ?? 0, e.Frequency)));

        return new ExpenseDefinitionListResponse
        {
            Data = expenses.Select(e => new ExpenseDefinitionItemResponse
            {
                Id = e.Id,
                Type = "expenseDefinition",
                Attributes = new ExpenseDefinitionAttributes
                {
                    Name = e.Name,
                    Currency = e.Currency,
                    AmountType = e.AmountType.ToString().ToLowerInvariant(),
                    AmountValue = e.AmountValue,
                    AmountFormula = e.AmountFormula,
                    Frequency = e.Frequency.ToString().ToLowerInvariant(),
                    Category = e.Category,
                    IsTaxDeductible = e.IsTaxDeductible,
                    LinkedAccountId = e.LinkedAccountId,
                    InflationAdjusted = e.InflationAdjusted,
                    IsActive = e.IsActive
                }
            }).ToList(),
            Meta = new ExpenseDefinitionMeta
            {
                TotalMonthly = totalMonthly,
                ByCategory = byCategory
            }
        };
    }

    private static decimal ConvertToMonthly(decimal amount, PaymentFrequency frequency)
    {
        return frequency switch
        {
            PaymentFrequency.Weekly => amount * 52m / 12m,
            PaymentFrequency.Biweekly => amount * 26m / 12m,
            PaymentFrequency.Monthly => amount,
            PaymentFrequency.Quarterly => amount / 3m,
            PaymentFrequency.Annually => amount / 12m,
            _ => amount
        };
    }
}
