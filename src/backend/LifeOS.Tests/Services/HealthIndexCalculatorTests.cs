using LifeOS.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LifeOS.Tests.Services;

/// <summary>
/// Unit tests for HealthIndexCalculator scoring functions (v3.0)
/// Tests all three per-metric scoring algorithms: AtOrBelow, AtOrAbove, Range
/// </summary>
public class HealthIndexCalculatorTests
{
    private class TestableHealthIndexCalculator : HealthIndexCalculator
    {
        public TestableHealthIndexCalculator() 
            : base(null!, null!, NullLogger<HealthIndexCalculator>.Instance)
        {
        }
        
        // Expose private methods for testing via public wrappers
        public decimal TestScoreAtOrBelow(decimal actualValue, decimal targetValue, decimal maxValue)
            => base.ScoreAtOrBelow(actualValue, targetValue, maxValue);
            
        public decimal TestScoreAtOrAbove(decimal actualValue, decimal targetValue, decimal minValue)
            => base.ScoreAtOrAbove(actualValue, targetValue, minValue);
            
        public decimal TestScoreRange(decimal actualValue, decimal minValue, decimal maxValue, decimal toleranceFactor = 0.2m)
            => base.ScoreRange(actualValue, minValue, maxValue, toleranceFactor);
    }

    private readonly TestableHealthIndexCalculator _calculator;

    public HealthIndexCalculatorTests()
    {
        _calculator = new TestableHealthIndexCalculator();
    }

    #region ScoreAtOrBelow Tests (Lower is Better)

    /// <summary>
    /// TEST: At target value returns perfect score
    /// Example: Resting HR of 60 (target) should be 100
    /// </summary>
    [Fact]
    public void ScoreAtOrBelow_AtTarget_Returns100()
    {
        // Resting heart rate: target=60, max=100
        var score = _calculator.TestScoreAtOrBelow(
            actualValue: 60m, 
            targetValue: 60m, 
            maxValue: 100m);
        
        Assert.Equal(100m, score);
    }

    /// <summary>
    /// TEST: Below target returns perfect score
    /// Example: Resting HR of 55 (below target 60) should be 100
    /// </summary>
    [Fact]
    public void ScoreAtOrBelow_BelowTarget_Returns100()
    {
        // Resting heart rate: 55 is better than target 60
        var score = _calculator.TestScoreAtOrBelow(
            actualValue: 55m, 
            targetValue: 60m, 
            maxValue: 100m);
        
        Assert.Equal(100m, score);
    }

    /// <summary>
    /// TEST: At maximum value returns worst score
    /// Example: Resting HR of 100 (max) should be 0
    /// </summary>
    [Fact]
    public void ScoreAtOrBelow_AtMax_Returns0()
    {
        // Resting heart rate at max (100) is worst case
        var score = _calculator.TestScoreAtOrBelow(
            actualValue: 100m, 
            targetValue: 60m, 
            maxValue: 100m);
        
        Assert.Equal(0m, score);
    }

    /// <summary>
    /// TEST: Above maximum returns worst score
    /// Example: Resting HR of 110 (above max 100) should be 0
    /// </summary>
    [Fact]
    public void ScoreAtOrBelow_AboveMax_Returns0()
    {
        // Resting heart rate above max (110 > 100)
        var score = _calculator.TestScoreAtOrBelow(
            actualValue: 110m, 
            targetValue: 60m, 
            maxValue: 100m);
        
        Assert.Equal(0m, score);
    }

    /// <summary>
    /// TEST: Midpoint between target and max returns 50
    /// Example: Resting HR of 80 (midpoint of 60 and 100) should be 50
    /// </summary>
    [Fact]
    public void ScoreAtOrBelow_Midpoint_Returns50()
    {
        // 80 is exactly halfway between target (60) and max (100)
        var score = _calculator.TestScoreAtOrBelow(
            actualValue: 80m, 
            targetValue: 60m, 
            maxValue: 100m);
        
        Assert.Equal(50m, score);
    }

    /// <summary>
    /// TEST: 25% between target and max
    /// Example: Resting HR of 70 should be 75
    /// </summary>
    [Fact]
    public void ScoreAtOrBelow_25PercentAboveTarget_Returns75()
    {
        // 70 is 25% of the way from 60 to 100
        // Score should be 75 (100 - 25)
        var score = _calculator.TestScoreAtOrBelow(
            actualValue: 70m, 
            targetValue: 60m, 
            maxValue: 100m);
        
        Assert.Equal(75m, score);
    }

    /// <summary>
    /// TEST: 75% between target and max
    /// Example: Resting HR of 90 should be 25
    /// </summary>
    [Fact]
    public void ScoreAtOrBelow_75PercentAboveTarget_Returns25()
    {
        // 90 is 75% of the way from 60 to 100
        // Score should be 25 (100 - 75)
        var score = _calculator.TestScoreAtOrBelow(
            actualValue: 90m, 
            targetValue: 60m, 
            maxValue: 100m);
        
        Assert.Equal(25m, score);
    }

    /// <summary>
    /// TEST: Decimal precision handling
    /// Example: Body weight 82.5 kg (target=75, max=100)
    /// </summary>
    [Fact]
    public void ScoreAtOrBelow_DecimalValues_CalculatesAccurately()
    {
        // 82.5 is 30% of the way from 75 to 100 (7.5 / 25 = 0.3)
        // Score should be 70 (100 - 30)
        var score = _calculator.TestScoreAtOrBelow(
            actualValue: 82.5m, 
            targetValue: 75m, 
            maxValue: 100m);
        
        Assert.Equal(70m, score);
    }

    #endregion

    #region ScoreAtOrAbove Tests (Higher is Better)

    /// <summary>
    /// TEST: At target value returns perfect score
    /// Example: HRV of 50 ms (target) should be 100
    /// </summary>
    [Fact]
    public void ScoreAtOrAbove_AtTarget_Returns100()
    {
        // HRV: target=50, min=20
        var score = _calculator.TestScoreAtOrAbove(
            actualValue: 50m, 
            targetValue: 50m, 
            minValue: 20m);
        
        Assert.Equal(100m, score);
    }

    /// <summary>
    /// TEST: Above target returns perfect score
    /// Example: HRV of 60 ms (above target 50) should be 100
    /// </summary>
    [Fact]
    public void ScoreAtOrAbove_AboveTarget_Returns100()
    {
        // HRV: 60 is better than target 50
        var score = _calculator.TestScoreAtOrAbove(
            actualValue: 60m, 
            targetValue: 50m, 
            minValue: 20m);
        
        Assert.Equal(100m, score);
    }

    /// <summary>
    /// TEST: At minimum value returns worst score
    /// Example: HRV of 20 ms (min) should be 0
    /// </summary>
    [Fact]
    public void ScoreAtOrAbove_AtMin_Returns0()
    {
        // HRV at min (20) is worst case
        var score = _calculator.TestScoreAtOrAbove(
            actualValue: 20m, 
            targetValue: 50m, 
            minValue: 20m);
        
        Assert.Equal(0m, score);
    }

    /// <summary>
    /// TEST: Below minimum returns worst score
    /// Example: HRV of 10 ms (below min 20) should be 0
    /// </summary>
    [Fact]
    public void ScoreAtOrAbove_BelowMin_Returns0()
    {
        // HRV below min (10 < 20)
        var score = _calculator.TestScoreAtOrAbove(
            actualValue: 10m, 
            targetValue: 50m, 
            minValue: 20m);
        
        Assert.Equal(0m, score);
    }

    /// <summary>
    /// TEST: Midpoint between min and target returns 50
    /// Example: HRV of 35 ms (midpoint of 20 and 50) should be 50
    /// </summary>
    [Fact]
    public void ScoreAtOrAbove_Midpoint_Returns50()
    {
        // 35 is exactly halfway between min (20) and target (50)
        var score = _calculator.TestScoreAtOrAbove(
            actualValue: 35m, 
            targetValue: 50m, 
            minValue: 20m);
        
        Assert.Equal(50m, score);
    }

    /// <summary>
    /// TEST: 25% between min and target
    /// Example: HRV of 27.5 ms should be 25
    /// </summary>
    [Fact]
    public void ScoreAtOrAbove_25PercentAboveMin_Returns25()
    {
        // 27.5 is 25% of the way from 20 to 50
        // Score should be 25
        var score = _calculator.TestScoreAtOrAbove(
            actualValue: 27.5m, 
            targetValue: 50m, 
            minValue: 20m);
        
        Assert.Equal(25m, score);
    }

    /// <summary>
    /// TEST: 75% between min and target
    /// Example: HRV of 42.5 ms should be 75
    /// </summary>
    [Fact]
    public void ScoreAtOrAbove_75PercentAboveMin_Returns75()
    {
        // 42.5 is 75% of the way from 20 to 50
        // Score should be 75
        var score = _calculator.TestScoreAtOrAbove(
            actualValue: 42.5m, 
            targetValue: 50m, 
            minValue: 20m);
        
        Assert.Equal(75m, score);
    }

    /// <summary>
    /// TEST: Very large value above target
    /// Example: HRV of 100 ms (well above target 50) should be 100
    /// </summary>
    [Fact]
    public void ScoreAtOrAbove_WellAboveTarget_Returns100()
    {
        // HRV: 100 is much better than target 50
        var score = _calculator.TestScoreAtOrAbove(
            actualValue: 100m, 
            targetValue: 50m, 
            minValue: 20m);
        
        Assert.Equal(100m, score);
    }

    #endregion

    #region ScoreRange Tests (Optimal Range)

    /// <summary>
    /// TEST: Within range returns perfect score
    /// Example: Body fat 14% (range 13-15%) should be 100
    /// </summary>
    [Fact]
    public void ScoreRange_WithinRange_Returns100()
    {
        // Body fat: range 13-15%
        var score = _calculator.TestScoreRange(
            actualValue: 14m, 
            minValue: 13m, 
            maxValue: 15m);
        
        Assert.Equal(100m, score);
    }

    /// <summary>
    /// TEST: At minimum boundary returns perfect score
    /// Example: Body fat 13% (exact lower bound) should be 100
    /// </summary>
    [Fact]
    public void ScoreRange_AtMinBoundary_Returns100()
    {
        var score = _calculator.TestScoreRange(
            actualValue: 13m, 
            minValue: 13m, 
            maxValue: 15m);
        
        Assert.Equal(100m, score);
    }

    /// <summary>
    /// TEST: At maximum boundary returns perfect score
    /// Example: Body fat 15% (exact upper bound) should be 100
    /// </summary>
    [Fact]
    public void ScoreRange_AtMaxBoundary_Returns100()
    {
        var score = _calculator.TestScoreRange(
            actualValue: 15m, 
            minValue: 13m, 
            maxValue: 15m);
        
        Assert.Equal(100m, score);
    }

    /// <summary>
    /// TEST: Just below range with 50% tolerance
    /// Example: Body fat 12.8% (0.2% below min) with range 13-15% (size=2, tolerance=0.4)
    /// Should be 50 (0.2 / 0.4 = 0.5, score = (1 - 0.5) * 100 = 50)
    /// </summary>
    [Fact]
    public void ScoreRange_HalfwayBelowMinInTolerance_Returns50()
    {
        // Range: 13-15 (size=2), tolerance=20% = 0.4
        // 12.8 is 0.2 below min (halfway through tolerance zone)
        // Score = (0.4 - 0.2) / 0.4 * 100 = 50
        var score = _calculator.TestScoreRange(
            actualValue: 12.8m, 
            minValue: 13m, 
            maxValue: 15m,
            toleranceFactor: 0.2m);
        
        Assert.Equal(50m, score);
    }

    /// <summary>
    /// TEST: Just above range with 50% tolerance
    /// Example: Body fat 15.2% (0.2% above max) should be 50
    /// </summary>
    [Fact]
    public void ScoreRange_HalfwayAboveMaxInTolerance_Returns50()
    {
        // Range: 13-15 (size=2), tolerance=20% = 0.4
        // 15.2 is 0.2 above max (halfway through tolerance zone)
        // Score = (0.4 - 0.2) / 0.4 * 100 = 50
        var score = _calculator.TestScoreRange(
            actualValue: 15.2m, 
            minValue: 13m, 
            maxValue: 15m,
            toleranceFactor: 0.2m);
        
        Assert.Equal(50m, score);
    }

    /// <summary>
    /// TEST: At tolerance boundary below range
    /// Example: Body fat 12.6% (0.4 below min, at tolerance edge) should be 0
    /// </summary>
    [Fact]
    public void ScoreRange_AtToleranceBoundaryBelow_Returns0()
    {
        // Range: 13-15 (size=2), tolerance=20% = 0.4
        // 12.6 is exactly 0.4 below min (at tolerance boundary)
        var score = _calculator.TestScoreRange(
            actualValue: 12.6m, 
            minValue: 13m, 
            maxValue: 15m,
            toleranceFactor: 0.2m);
        
        Assert.Equal(0m, score);
    }

    /// <summary>
    /// TEST: At tolerance boundary above range
    /// Example: Body fat 15.4% (0.4 above max, at tolerance edge) should be 0
    /// </summary>
    [Fact]
    public void ScoreRange_AtToleranceBoundaryAbove_Returns0()
    {
        // Range: 13-15 (size=2), tolerance=20% = 0.4
        // 15.4 is exactly 0.4 above max (at tolerance boundary)
        var score = _calculator.TestScoreRange(
            actualValue: 15.4m, 
            minValue: 13m, 
            maxValue: 15m,
            toleranceFactor: 0.2m);
        
        Assert.Equal(0m, score);
    }

    /// <summary>
    /// TEST: Beyond tolerance below range
    /// Example: Body fat 11% (far below range) should be 0
    /// </summary>
    [Fact]
    public void ScoreRange_FarBelowRange_Returns0()
    {
        // 11 is more than 0.4 below min (13)
        var score = _calculator.TestScoreRange(
            actualValue: 11m, 
            minValue: 13m, 
            maxValue: 15m);
        
        Assert.Equal(0m, score);
    }

    /// <summary>
    /// TEST: Beyond tolerance above range
    /// Example: Body fat 18% (far above range) should be 0
    /// </summary>
    [Fact]
    public void ScoreRange_FarAboveRange_Returns0()
    {
        // 18 is more than 0.4 above max (15)
        var score = _calculator.TestScoreRange(
            actualValue: 18m, 
            minValue: 13m, 
            maxValue: 15m);
        
        Assert.Equal(0m, score);
    }

    /// <summary>
    /// TEST: Custom tolerance factor 10%
    /// Example: Blood glucose range 70-100 mg/dL with 10% tolerance
    /// </summary>
    [Fact]
    public void ScoreRange_CustomTolerance10Percent_CalculatesCorrectly()
    {
        // Range: 70-100 (size=30), tolerance=10% = 3
        // 68 is 2 below min (within tolerance of 3)
        // Score = (3 - 2) / 3 * 100 = 33.33...
        var score = _calculator.TestScoreRange(
            actualValue: 68m, 
            minValue: 70m, 
            maxValue: 100m,
            toleranceFactor: 0.1m);
        
        Assert.Equal(33.333333333333333333333333m, score, precision: 10);
    }

    /// <summary>
    /// TEST: Custom tolerance factor 50%
    /// Example: Very forgiving range scoring
    /// </summary>
    [Fact]
    public void ScoreRange_CustomTolerance50Percent_CalculatesCorrectly()
    {
        // Range: 13-15 (size=2), tolerance=50% = 1
        // 12 is 1 below min (at edge of tolerance)
        var score = _calculator.TestScoreRange(
            actualValue: 12m, 
            minValue: 13m, 
            maxValue: 15m,
            toleranceFactor: 0.5m);
        
        Assert.Equal(0m, score);
    }

    /// <summary>
    /// TEST: Zero tolerance factor (strict range)
    /// Example: Must be exactly in range or score is 0
    /// </summary>
    [Fact]
    public void ScoreRange_ZeroTolerance_OnlyInRangeScores()
    {
        // Range: 13-15, tolerance=0
        // 12.99 is just outside range
        var score = _calculator.TestScoreRange(
            actualValue: 12.99m, 
            minValue: 13m, 
            maxValue: 15m,
            toleranceFactor: 0m);
        
        Assert.Equal(0m, score);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// TEST: Zero values handled correctly in AtOrBelow
    /// </summary>
    [Fact]
    public void ScoreAtOrBelow_ZeroValues_HandlesCorrectly()
    {
        // Target=0, Max=100, Actual=0 should be perfect
        var score = _calculator.TestScoreAtOrBelow(
            actualValue: 0m, 
            targetValue: 0m, 
            maxValue: 100m);
        
        Assert.Equal(100m, score);
    }

    /// <summary>
    /// TEST: Zero values handled correctly in AtOrAbove
    /// </summary>
    [Fact]
    public void ScoreAtOrAbove_ZeroMin_HandlesCorrectly()
    {
        // Min=0, Target=100, Actual=50 should be 50
        var score = _calculator.TestScoreAtOrAbove(
            actualValue: 50m, 
            targetValue: 100m, 
            minValue: 0m);
        
        Assert.Equal(50m, score);
    }

    /// <summary>
    /// TEST: Very small range difference
    /// </summary>
    [Fact]
    public void ScoreRange_VerySmallRange_HandlesCorrectly()
    {
        // Range: 99.5-100 (size=0.5), tolerance=20% = 0.1
        // 99.6 is 0.1 outside range (at tolerance boundary)
        var score = _calculator.TestScoreRange(
            actualValue: 99.4m, 
            minValue: 99.5m, 
            maxValue: 100m,
            toleranceFactor: 0.2m);
        
        // 99.4 is 0.1 below min, which is at the tolerance edge
        Assert.Equal(0m, score);
    }

    /// <summary>
    /// TEST: Large numbers don't cause overflow
    /// </summary>
    [Fact]
    public void ScoreAtOrAbove_LargeNumbers_NoOverflow()
    {
        // Very large values (e.g., annual income in cents)
        var score = _calculator.TestScoreAtOrAbove(
            actualValue: 5000000m, 
            targetValue: 10000000m, 
            minValue: 0m);
        
        Assert.Equal(50m, score);
    }

    #endregion
}
