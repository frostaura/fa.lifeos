namespace LifeOS.Application.Services;

/// <summary>
/// Parser for simulation condition expressions.
/// Supports simple expressions like: netWorth >= 1000000, age >= 40, assets > liabilities
/// </summary>
public interface IConditionParser
{
    /// <summary>
    /// Evaluate a condition expression against simulation state
    /// </summary>
    bool Evaluate(string condition, SimulationState state);
}

public class SimulationState
{
    public decimal NetWorth { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public int Age { get; set; }
    public DateOnly CurrentDate { get; set; }
    public int MonthsElapsed { get; set; }
    public Dictionary<Guid, decimal> AccountBalances { get; set; } = new();
    public decimal TotalMonthlyIncome { get; set; }
    public decimal TotalMonthlyExpenses { get; set; }
}

public class ConditionParser : IConditionParser
{
    public bool Evaluate(string condition, SimulationState state)
    {
        if (string.IsNullOrWhiteSpace(condition))
            return false;

        // Normalize condition
        condition = condition.Trim().ToLowerInvariant();

        // Try to parse: variable operator value
        var operators = new[] { ">=", "<=", "!=", "==", "=", ">", "<" };
        
        foreach (var op in operators)
        {
            var parts = condition.Split(op, 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                var variable = parts[0];
                var valueStr = parts[1];
                
                var leftValue = GetVariableValue(variable, state);
                if (!leftValue.HasValue)
                    continue;

                if (!decimal.TryParse(valueStr.Replace(",", "").Replace("_", ""), out var rightValue))
                    continue;

                return op switch
                {
                    ">=" => leftValue.Value >= rightValue,
                    "<=" => leftValue.Value <= rightValue,
                    "!=" => leftValue.Value != rightValue,
                    "==" or "=" => leftValue.Value == rightValue,
                    ">" => leftValue.Value > rightValue,
                    "<" => leftValue.Value < rightValue,
                    _ => false
                };
            }
        }

        return false;
    }

    private decimal? GetVariableValue(string variable, SimulationState state)
    {
        return variable.Trim().ToLowerInvariant() switch
        {
            "networth" or "net_worth" or "net-worth" => state.NetWorth,
            "assets" or "totalassets" or "total_assets" => state.TotalAssets,
            "liabilities" or "totalliabilities" or "total_liabilities" => state.TotalLiabilities,
            "age" => state.Age,
            "months" or "monthselapsed" or "months_elapsed" => state.MonthsElapsed,
            "income" or "monthlyincome" or "monthly_income" => state.TotalMonthlyIncome,
            "expenses" or "monthlyexpenses" or "monthly_expenses" => state.TotalMonthlyExpenses,
            _ => null
        };
    }
}
