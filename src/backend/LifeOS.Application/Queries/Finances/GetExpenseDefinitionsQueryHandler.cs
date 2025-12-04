using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Finances;
using LifeOS.Domain.Entities;
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
            .Include(e => e.LinkedAccount)
            .Where(e => e.UserId == request.UserId)
            .OrderBy(e => e.Category)
            .ThenBy(e => e.Name)
            .ToListAsync(cancellationToken);

        var activeExpenses = expenses.Where(e => e.IsActive).ToList();
        var currentMonth = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalMonthly = activeExpenses.Sum(e => ConvertToMonthlyWithOnceOff(e, currentMonth));
        
        var byCategory = activeExpenses
            .GroupBy(e => e.Category)
            .ToDictionary(
                g => g.Key, 
                g => g.Sum(e => ConvertToMonthlyWithOnceOff(e, currentMonth)));

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
                    StartDate = e.StartDate?.ToString("yyyy-MM-dd"),
                    Category = e.Category,
                    IsTaxDeductible = e.IsTaxDeductible,
                    LinkedAccountId = e.LinkedAccountId,
                    LinkedAccountName = e.LinkedAccount?.Name,
                    InflationAdjusted = e.InflationAdjusted,
                    IsActive = e.IsActive,
                    EndConditionType = e.EndConditionType.ToString().ToLowerInvariant(),
                    EndConditionAccountId = e.EndConditionAccountId,
                    EndDate = e.EndDate?.ToString("yyyy-MM-dd"),
                    EndAmountThreshold = e.EndAmountThreshold
                }
            }).ToList(),
            Meta = new ExpenseDefinitionMeta
            {
                TotalMonthly = totalMonthly,
                ByCategory = byCategory
            }
        };
    }

    /// <summary>
    /// Converts an expense amount to its monthly equivalent, with special handling for once-off expenses.
    /// Once-off expenses are included in the monthly total if their StartDate falls within the specified month.
    /// </summary>
    private static decimal ConvertToMonthlyWithOnceOff(ExpenseDefinition expense, DateOnly currentMonth)
    {
        var amount = expense.AmountValue ?? 0;
        
        if (expense.Frequency == PaymentFrequency.Once)
        {
            // For once-off expenses, include the full amount if StartDate is in the current month
            if (expense.StartDate.HasValue)
            {
                var startDate = expense.StartDate.Value;
                if (startDate.Year == currentMonth.Year && startDate.Month == currentMonth.Month)
                {
                    return amount;
                }
            }
            return 0m; // Once-off not in current month
        }
        
        return expense.Frequency switch
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
