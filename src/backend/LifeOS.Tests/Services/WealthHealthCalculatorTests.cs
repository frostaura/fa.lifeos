using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LifeOS.Tests.Services;

/// <summary>
/// Unit tests for WealthHealthCalculator scoring functions
/// Tests individual component scoring algorithms in isolation
/// </summary>
public class WealthHealthCalculatorTests
{
    private class TestableWealthHealthCalculator : WealthHealthCalculator
    {
        public TestableWealthHealthCalculator() 
            : base(new Mock<ILifeOSDbContext>().Object, NullLogger<WealthHealthCalculator>.Instance)
        {
        }

        // Expose protected methods for testing
        public new decimal ScoreSavingsRate(decimal actualRate) => base.ScoreSavingsRate(actualRate);
        public new decimal ScoreDebtToIncome(decimal ratio) => base.ScoreDebtToIncome(ratio);
        public new decimal ScoreEmergencyFund(decimal months) => base.ScoreEmergencyFund(months);
        public new decimal ScoreNetWorthGrowth(decimal growthRate) => base.ScoreNetWorthGrowth(growthRate);
    }

    private readonly TestableWealthHealthCalculator _calculator = new();

    #region Savings Rate Tests

    [Fact]
    public void ScoreSavingsRate_AtTarget_Returns100()
    {
        var score = _calculator.ScoreSavingsRate(0.30m); // 30%
        Assert.Equal(100m, score);
    }

    [Fact]
    public void ScoreSavingsRate_AboveTarget_Returns100Capped()
    {
        var score = _calculator.ScoreSavingsRate(0.50m); // 50%
        Assert.Equal(100m, score); // Capped at 100
    }

    [Fact]
    public void ScoreSavingsRate_HalfTarget_Returns50()
    {
        var score = _calculator.ScoreSavingsRate(0.15m); // 15%
        Assert.Equal(50m, score);
    }

    [Fact]
    public void ScoreSavingsRate_Zero_Returns0()
    {
        var score = _calculator.ScoreSavingsRate(0m);
        Assert.Equal(0m, score);
    }

    [Fact]
    public void ScoreSavingsRate_Negative_Returns0Clamped()
    {
        var score = _calculator.ScoreSavingsRate(-0.10m);
        Assert.Equal(0m, score); // Clamped to 0
    }

    #endregion

    #region Debt-to-Income Tests

    [Fact]
    public void ScoreDebtToIncome_Zero_Returns100()
    {
        var score = _calculator.ScoreDebtToIncome(0m);
        Assert.Equal(100m, score);
    }

    [Fact]
    public void ScoreDebtToIncome_At30Percent_Returns100()
    {
        var score = _calculator.ScoreDebtToIncome(0.30m);
        Assert.Equal(100m, score);
    }

    [Fact]
    public void ScoreDebtToIncome_At65Percent_Returns50()
    {
        // 65% is midpoint between 30% (good) and 100% (bad)
        var score = _calculator.ScoreDebtToIncome(0.65m);
        Assert.Equal(50m, score);
    }

    [Fact]
    public void ScoreDebtToIncome_At100Percent_Returns0()
    {
        var score = _calculator.ScoreDebtToIncome(1.00m);
        Assert.Equal(0m, score);
    }

    [Fact]
    public void ScoreDebtToIncome_Above100Percent_Returns0()
    {
        var score = _calculator.ScoreDebtToIncome(1.50m);
        Assert.Equal(0m, score);
    }

    [Fact]
    public void ScoreDebtToIncome_LinearDecay()
    {
        // Test linear interpolation
        var score45 = _calculator.ScoreDebtToIncome(0.45m);
        var score80 = _calculator.ScoreDebtToIncome(0.80m);
        
        Assert.True(score45 > score80); // Higher debt = lower score
        Assert.True(score45 < 100m && score45 > 0m);
        Assert.True(score80 < 100m && score80 > 0m);
    }

    #endregion

    #region Emergency Fund Tests

    [Fact]
    public void ScoreEmergencyFund_Zero_Returns0()
    {
        var score = _calculator.ScoreEmergencyFund(0m);
        Assert.Equal(0m, score);
    }

    [Fact]
    public void ScoreEmergencyFund_ThreeMonths_Returns50()
    {
        var score = _calculator.ScoreEmergencyFund(3m);
        Assert.Equal(50m, score);
    }

    [Fact]
    public void ScoreEmergencyFund_SixMonths_Returns100()
    {
        var score = _calculator.ScoreEmergencyFund(6m);
        Assert.Equal(100m, score);
    }

    [Fact]
    public void ScoreEmergencyFund_TwelveMonths_Returns100Capped()
    {
        var score = _calculator.ScoreEmergencyFund(12m);
        Assert.Equal(100m, score); // Capped at 100
    }

    [Fact]
    public void ScoreEmergencyFund_OneMonth_ReturnsCorrect()
    {
        var score = _calculator.ScoreEmergencyFund(1m);
        Assert.InRange(score, 16m, 17m); // ~16.67
    }

    #endregion

    #region Net Worth Growth Tests

    [Fact]
    public void ScoreNetWorthGrowth_AtTarget_Returns100()
    {
        var score = _calculator.ScoreNetWorthGrowth(0.08m); // 8%
        Assert.Equal(100m, score);
    }

    [Fact]
    public void ScoreNetWorthGrowth_AboveTarget_Returns100Capped()
    {
        var score = _calculator.ScoreNetWorthGrowth(0.15m); // 15%
        Assert.Equal(100m, score); // Capped at 100
    }

    [Fact]
    public void ScoreNetWorthGrowth_HalfTarget_Returns50()
    {
        var score = _calculator.ScoreNetWorthGrowth(0.04m); // 4%
        Assert.Equal(50m, score);
    }

    [Fact]
    public void ScoreNetWorthGrowth_Zero_Returns0()
    {
        var score = _calculator.ScoreNetWorthGrowth(0m);
        Assert.Equal(0m, score);
    }

    [Fact]
    public void ScoreNetWorthGrowth_Negative_Returns0Clamped()
    {
        var score = _calculator.ScoreNetWorthGrowth(-0.05m); // -5%
        Assert.Equal(0m, score); // Clamped to 0
    }

    [Fact]
    public void ScoreNetWorthGrowth_SlightlyBelowTarget_ReturnsProportional()
    {
        var score = _calculator.ScoreNetWorthGrowth(0.06m); // 6% (75% of 8% target)
        Assert.Equal(75m, score);
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData(0.00)]
    [InlineData(0.15)]
    [InlineData(0.30)]
    [InlineData(0.50)]
    [InlineData(1.00)]
    public void AllScoringFunctions_ReturnValidRange(decimal input)
    {
        var savingsScore = _calculator.ScoreSavingsRate(input);
        var debtScore = _calculator.ScoreDebtToIncome(input);
        var emergencyScore = _calculator.ScoreEmergencyFund(input * 10); // Scale up for months
        var growthScore = _calculator.ScoreNetWorthGrowth(input);

        Assert.InRange(savingsScore, 0m, 100m);
        Assert.InRange(debtScore, 0m, 100m);
        Assert.InRange(emergencyScore, 0m, 100m);
        Assert.InRange(growthScore, 0m, 100m);
    }

    [Fact]
    public void ScoringFunctions_HandleDecimalPrecision()
    {
        // Test with high-precision decimals
        var score1 = _calculator.ScoreSavingsRate(0.299999m);
        var score2 = _calculator.ScoreSavingsRate(0.300001m);

        Assert.True(score1 < 100m);
        Assert.True(score2 >= 100m);
    }

    #endregion
}
