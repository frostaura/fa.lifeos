using LifeOS.Application.Services;
using Xunit;

namespace LifeOS.Tests.Services;

public class ConditionParserTests
{
    private readonly IConditionParser _parser;

    public ConditionParserTests()
    {
        _parser = new ConditionParser();
    }

    [Fact]
    public void Evaluate_NetWorthGreaterThanOrEqual_ReturnsTrue()
    {
        var state = new SimulationState { NetWorth = 1000000 };
        
        var result = _parser.Evaluate("netWorth >= 1000000", state);
        
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_NetWorthGreaterThanOrEqual_ReturnsFalse()
    {
        var state = new SimulationState { NetWorth = 999999 };
        
        var result = _parser.Evaluate("netWorth >= 1000000", state);
        
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_AgeGreaterThanOrEqual_ReturnsTrue()
    {
        var state = new SimulationState { Age = 40 };
        
        var result = _parser.Evaluate("age >= 40", state);
        
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_AssetsGreaterThanLiabilities_ReturnsTrue()
    {
        var state = new SimulationState { TotalAssets = 500000, TotalLiabilities = 200000 };
        
        var result = _parser.Evaluate("assets > 200000", state);
        
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_NetWorthWithUnderscore_Works()
    {
        var state = new SimulationState { NetWorth = 1500000 };
        
        var result = _parser.Evaluate("net_worth >= 1_000_000", state);
        
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_LessThan_ReturnsCorrectResult()
    {
        var state = new SimulationState { Age = 35 };
        
        Assert.True(_parser.Evaluate("age < 40", state));
        Assert.False(_parser.Evaluate("age < 35", state));
    }

    [Fact]
    public void Evaluate_Equals_ReturnsCorrectResult()
    {
        var state = new SimulationState { MonthsElapsed = 12 };
        
        Assert.True(_parser.Evaluate("months == 12", state));
        Assert.True(_parser.Evaluate("months = 12", state));
        Assert.False(_parser.Evaluate("months == 13", state));
    }

    [Fact]
    public void Evaluate_NotEquals_ReturnsCorrectResult()
    {
        var state = new SimulationState { Age = 35 };
        
        Assert.True(_parser.Evaluate("age != 40", state));
        Assert.False(_parser.Evaluate("age != 35", state));
    }

    [Fact]
    public void Evaluate_EmptyCondition_ReturnsFalse()
    {
        var state = new SimulationState();
        
        Assert.False(_parser.Evaluate("", state));
        Assert.False(_parser.Evaluate(null!, state));
        Assert.False(_parser.Evaluate("   ", state));
    }

    [Fact]
    public void Evaluate_InvalidVariable_ReturnsFalse()
    {
        var state = new SimulationState { NetWorth = 1000000 };
        
        var result = _parser.Evaluate("unknownVar >= 1000000", state);
        
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_CaseInsensitive_Works()
    {
        var state = new SimulationState { NetWorth = 1000000 };
        
        Assert.True(_parser.Evaluate("NETWORTH >= 1000000", state));
        Assert.True(_parser.Evaluate("NetWorth >= 1000000", state));
        Assert.True(_parser.Evaluate("networth >= 1000000", state));
    }

    [Fact]
    public void Evaluate_MonthlyIncome_Works()
    {
        var state = new SimulationState { TotalMonthlyIncome = 50000 };
        
        Assert.True(_parser.Evaluate("income >= 50000", state));
        Assert.True(_parser.Evaluate("monthly_income >= 50000", state));
    }

    [Fact]
    public void Evaluate_MonthlyExpenses_Works()
    {
        var state = new SimulationState { TotalMonthlyExpenses = 30000 };
        
        Assert.True(_parser.Evaluate("expenses >= 30000", state));
        Assert.True(_parser.Evaluate("monthly_expenses >= 30000", state));
    }
}
