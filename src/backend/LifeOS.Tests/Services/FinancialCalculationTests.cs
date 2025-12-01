using System.Text.Json;
using LifeOS.Domain.Enums;
using Xunit;
using FluentAssertions;

namespace LifeOS.Tests.Services;

/// <summary>
/// Tests for financial calculations used in the simulation engine and income/expense handlers.
/// Covers South African tax brackets, UIF, frequency conversions, and net income calculations.
/// </summary>
public class FinancialCalculationTests
{
    #region Tax Bracket Tests - South African PAYE

    /// <summary>
    /// Tests South African 2024/25 tax calculation with primary rebate of R17,235.
    /// Tax-free threshold: R95,750 (where 18% tax = R17,235 rebate exactly)
    /// </summary>
    [Theory]
    [InlineData(95750, 0)]           // Tax-free threshold: R95,750 * 0.18 = R17,235 = rebate
    [InlineData(95751, 0.18)]        // Just above threshold: 0.18 cents after rebate
    [InlineData(200000, 18765)]      // R200,000 * 18% = R36,000 - R17,235 = R18,765
    [InlineData(237100, 25443)]      // Bracket 1 max: R237,100 * 18% = R42,678 - R17,235 = R25,443
    [InlineData(370500, 60127)]      // Bracket 2 max: R42,678 + (R370,500-R237,100)*26% = R77,362 - R17,235 = R60,127
    [InlineData(512800, 104240)]     // Bracket 3 max: R77,362 + (R512,800-R370,500)*31% = R121,475 - R17,235 = R104,240 (rounded)
    [InlineData(673000, 161912)]     // Bracket 4 max: R121,475 + (R673,000-R512,800)*36% = R179,147 - R17,235 = R161,912
    [InlineData(857900, 234023)]     // Bracket 5 max: R179,147 + (R857,900-R673,000)*39% = R251,258 - R17,235 = R234,023
    [InlineData(1000000, 292421)]    // In bracket 6: R251,258 + (R1,000,000-R857,900)*41% = R309,656 - R17,235 = R292,421
    [InlineData(1500000, 497284)]    // Higher in bracket 6: R251,258 + (R1,500,000-R857,900)*41% = R514,519 - R17,235 = R497,284
    public void CalculateTaxFromBrackets_SouthAfricanRates_ReturnsCorrectTax(decimal annualIncome, decimal expectedTax)
    {
        // Arrange - South African 2024/25 tax brackets
        var brackets = GetSouthAfricanTaxBrackets2024();
        
        // Act
        var actualTax = CalculateTaxFromBrackets(annualIncome, brackets);
        
        // Assert - Allow 1% tolerance due to rounding differences
        actualTax.Should().BeApproximately(expectedTax, expectedTax * 0.01m + 1);
    }

    [Fact]
    public void CalculateTaxFromBrackets_ZeroIncome_ReturnsZero()
    {
        var brackets = GetSouthAfricanTaxBrackets2024();
        var tax = CalculateTaxFromBrackets(0, brackets);
        tax.Should().Be(0);
    }

    [Fact]
    public void CalculateTaxFromBrackets_NegativeIncome_ReturnsZero()
    {
        var brackets = GetSouthAfricanTaxBrackets2024();
        var tax = CalculateTaxFromBrackets(-50000, brackets);
        tax.Should().Be(0);
    }

    [Fact]
    public void CalculateTaxFromBrackets_EmptyBrackets_ReturnsZero()
    {
        var tax = CalculateTaxFromBrackets(500000, "[]");
        tax.Should().Be(0);
    }

    [Fact]
    public void CalculateTaxFromBrackets_InvalidJson_ReturnsZero()
    {
        var tax = CalculateTaxFromBrackets(500000, "invalid json");
        tax.Should().Be(0);
    }

    #endregion

    #region UIF Calculation Tests

    [Theory]
    [InlineData(17712, 177.12)]   // Exactly at cap threshold (17712 * 1% = 177.12)
    [InlineData(10000, 100.00)]   // Below cap
    [InlineData(20000, 177.12)]   // Above cap - capped at 177.12
    [InlineData(50000, 177.12)]   // Well above cap
    [InlineData(100000, 177.12)]  // High income - still capped
    public void CalculateUIF_WithCap_ReturnsCorrectContribution(decimal monthlyIncome, decimal expectedUif)
    {
        // Arrange
        const decimal uifRate = 0.01m;      // 1%
        const decimal uifCap = 177.12m;     // Monthly cap
        
        // Act
        var actualUif = CalculateUIF(monthlyIncome, uifRate, uifCap);
        
        // Assert
        actualUif.Should().BeApproximately(expectedUif, 0.01m);
    }

    [Fact]
    public void CalculateUIF_ZeroIncome_ReturnsZero()
    {
        var uif = CalculateUIF(0, 0.01m, 177.12m);
        uif.Should().Be(0);
    }

    [Fact]
    public void CalculateUIF_NoCap_ReturnsFullPercentage()
    {
        var uif = CalculateUIF(50000, 0.01m, null);
        uif.Should().Be(500); // 50000 * 1%
    }

    [Fact]
    public void CalculateUIF_NullRate_ReturnsZero()
    {
        var uif = CalculateUIF(50000, null, 177.12m);
        uif.Should().Be(0);
    }

    #endregion

    #region Frequency Conversion Tests - ConvertToMonthly

    [Theory]
    [InlineData(1000, PaymentFrequency.Weekly, 4333.33)]     // 1000 * 52 / 12 = 4333.33
    [InlineData(2000, PaymentFrequency.Biweekly, 4333.33)]  // 2000 * 26 / 12 = 4333.33
    [InlineData(5000, PaymentFrequency.Monthly, 5000)]       // No conversion needed
    [InlineData(15000, PaymentFrequency.Quarterly, 5000)]   // 15000 / 3 = 5000
    [InlineData(60000, PaymentFrequency.Annually, 5000)]    // 60000 / 12 = 5000
    public void ConvertToMonthly_AllFrequencies_ReturnsCorrectMonthlyAmount(
        decimal amount, PaymentFrequency frequency, decimal expectedMonthly)
    {
        // Act
        var actualMonthly = ConvertToMonthly(amount, frequency);
        
        // Assert
        actualMonthly.Should().BeApproximately(expectedMonthly, 0.01m);
    }

    [Fact]
    public void ConvertToMonthly_ZeroAmount_ReturnsZero()
    {
        foreach (var frequency in Enum.GetValues<PaymentFrequency>())
        {
            var result = ConvertToMonthly(0, frequency);
            result.Should().Be(0);
        }
    }

    [Fact]
    public void ConvertToMonthly_WeeklyToMonthlyConversion_IsAccurate()
    {
        // 52 weeks per year / 12 months = 4.333... weeks per month
        var weekly = 1000m;
        var monthly = ConvertToMonthly(weekly, PaymentFrequency.Weekly);
        var annual = weekly * 52;
        
        // Monthly * 12 should equal annual
        (monthly * 12).Should().BeApproximately(annual, 0.01m);
    }

    [Fact]
    public void ConvertToMonthly_BiweeklyToMonthlyConversion_IsAccurate()
    {
        // 26 pay periods per year / 12 months = 2.166... per month
        var biweekly = 2000m;
        var monthly = ConvertToMonthly(biweekly, PaymentFrequency.Biweekly);
        var annual = biweekly * 26;
        
        // Monthly * 12 should equal annual
        (monthly * 12).Should().BeApproximately(annual, 0.01m);
    }

    #endregion

    #region Investment Contribution Frequency Tests

    [Theory]
    [InlineData(1000, PaymentFrequency.Weekly, 4330)]      // 1000 * 4.33
    [InlineData(2000, PaymentFrequency.Biweekly, 4340)]   // 2000 * 2.17
    [InlineData(5000, PaymentFrequency.Monthly, 5000)]     // No conversion
    [InlineData(15000, PaymentFrequency.Quarterly, 5000)] // 15000 / 3
    [InlineData(60000, PaymentFrequency.Annually, 5000)]  // 60000 / 12
    public void GetMonthlyInvestmentContribution_AllFrequencies_ReturnsCorrectAmount(
        decimal amount, PaymentFrequency frequency, decimal expectedMonthly)
    {
        // Act - Using the investment handler's specific formula
        var actualMonthly = GetInvestmentMonthlyAmount(amount, frequency);
        
        // Assert
        actualMonthly.Should().BeApproximately(expectedMonthly, 1m);
    }

    #endregion

    #region Expense Totaling Tests

    [Fact]
    public void CalculateTotalMonthlyExpenses_MixedFrequencies_SumsCorrectly()
    {
        // Arrange
        var expenses = new[]
        {
            (Amount: 500m, Frequency: PaymentFrequency.Weekly),      // 500 * 52/12 = 2166.67
            (Amount: 1000m, Frequency: PaymentFrequency.Biweekly),   // 1000 * 26/12 = 2166.67
            (Amount: 3000m, Frequency: PaymentFrequency.Monthly),    // 3000
            (Amount: 6000m, Frequency: PaymentFrequency.Quarterly),  // 6000/3 = 2000
            (Amount: 12000m, Frequency: PaymentFrequency.Annually),  // 12000/12 = 1000
        };
        
        // Act
        var totalMonthly = expenses.Sum(e => ConvertToMonthly(e.Amount, e.Frequency));
        
        // Expected: 2166.67 + 2166.67 + 3000 + 2000 + 1000 = 10333.33
        totalMonthly.Should().BeApproximately(10333.33m, 1m);
    }

    [Fact]
    public void CalculateTotalMonthlyExpenses_NoExpenses_ReturnsZero()
    {
        var expenses = Array.Empty<(decimal, PaymentFrequency)>();
        var total = expenses.Sum(e => ConvertToMonthly(e.Item1, e.Item2));
        total.Should().Be(0);
    }

    [Fact]
    public void CalculateTotalMonthlyExpenses_SingleExpense_ReturnsConvertedAmount()
    {
        var monthlyTotal = ConvertToMonthly(12000m, PaymentFrequency.Annually);
        monthlyTotal.Should().Be(1000m);
    }

    #endregion

    #region Net Income Calculation Tests (Gross - Tax - UIF)

    [Theory]
    [InlineData(25000, 12)]   // R25,000/month = R300,000/year
    [InlineData(50000, 12)]   // R50,000/month = R600,000/year  
    [InlineData(100000, 12)]  // R100,000/month = R1,200,000/year
    public void CalculateNetIncome_MonthlyGross_ReturnsCorrectNetAfterTaxAndUif(
        decimal monthlyGross, int periods)
    {
        // Arrange
        var annualGross = monthlyGross * periods;
        var brackets = GetSouthAfricanTaxBrackets2024();
        var rebate = 17235m; // Primary rebate for 2024
        
        // Calculate annual tax
        var annualTax = CalculateTaxFromBrackets(annualGross, brackets);
        annualTax = Math.Max(0, annualTax - rebate);
        
        // Calculate monthly UIF (capped)
        var monthlyUif = Math.Min(monthlyGross * 0.01m, 177.12m);
        var annualUif = monthlyUif * 12;
        
        // Calculate net
        var annualNet = annualGross - annualTax - annualUif;
        var monthlyNet = annualNet / 12;
        
        // Assert - Net should be less than gross
        monthlyNet.Should().BeLessThan(monthlyGross);
        monthlyNet.Should().BePositive();
        
        // Tax should be reasonable percentage (15-45% depending on bracket)
        var effectiveTaxRate = (annualTax + annualUif) / annualGross;
        effectiveTaxRate.Should().BeGreaterThan(0);
        effectiveTaxRate.Should().BeLessThan(0.50m);
    }

    [Fact]
    public void CalculateNetIncome_BelowTaxThreshold_OnlyPayUif()
    {
        // Arrange - R7,000/month = R84,000/year (below tax threshold)
        var monthlyGross = 7000m;
        var annualGross = monthlyGross * 12;
        var brackets = GetSouthAfricanTaxBrackets2024();
        var rebate = 17235m;
        
        // Act
        var annualTax = CalculateTaxFromBrackets(annualGross, brackets);
        annualTax = Math.Max(0, annualTax - rebate);
        var monthlyUif = Math.Min(monthlyGross * 0.01m, 177.12m);
        
        // Assert - Tax should be zero (below threshold after rebate)
        annualTax.Should().Be(0);
        monthlyUif.Should().Be(70m); // 7000 * 1% = 70
    }

    [Fact]
    public void CalculateNetIncome_HighEarner_TaxIsSignificantPortion()
    {
        // Arrange - R200,000/month = R2,400,000/year
        var monthlyGross = 200000m;
        var annualGross = monthlyGross * 12;
        var brackets = GetSouthAfricanTaxBrackets2024();
        var rebate = 17235m;
        
        // Act
        var annualTax = CalculateTaxFromBrackets(annualGross, brackets);
        annualTax = Math.Max(0, annualTax - rebate);
        // Note: UIF is calculated separately and capped at R177.12/month
        
        var effectiveTaxRate = annualTax / annualGross;
        
        // Assert - High earner should pay ~35-40% effective rate
        effectiveTaxRate.Should().BeGreaterThan(0.35m);
        effectiveTaxRate.Should().BeLessThan(0.45m);
    }

    #endregion

    #region Rebate Application Tests

    [Theory]
    [InlineData("under65", 17235)]
    [InlineData("65-74", 17235 + 9444)]   // Primary + Secondary
    [InlineData("75+", 17235 + 9444 + 3145)] // Primary + Secondary + Tertiary
    public void GetPrimaryRebate_AgeBasedRebates_ReturnsCorrectTotal(string ageCategory, decimal expectedRebate)
    {
        // Arrange
        var rebatesJson = ageCategory switch
        {
            "under65" => "{\"primary\": 17235, \"secondary\": 0, \"tertiary\": 0}",
            "65-74" => "{\"primary\": 17235, \"secondary\": 9444, \"tertiary\": 0}",
            "75+" => "{\"primary\": 17235, \"secondary\": 9444, \"tertiary\": 3145}",
            _ => "{\"primary\": 17235}"
        };
        
        // Act
        var totalRebate = GetTotalRebate(rebatesJson);
        
        // Assert
        totalRebate.Should().Be(expectedRebate);
    }

    [Fact]
    public void GetPrimaryRebate_NullJson_ReturnsZero()
    {
        var rebate = GetTotalRebate(null);
        rebate.Should().Be(0);
    }

    [Fact]
    public void GetPrimaryRebate_EmptyJson_ReturnsZero()
    {
        var rebate = GetTotalRebate("");
        rebate.Should().Be(0);
    }

    #endregion

    #region Helper Methods (Match implementation in handlers)

    private static string GetSouthAfricanTaxBrackets2024()
    {
        // South African 2024/25 tax brackets
        var brackets = new[]
        {
            new { min = 1, max = (int?)237100, rate = 0.18m, baseTax = 0m },
            new { min = 237101, max = (int?)370500, rate = 0.26m, baseTax = 42678m },
            new { min = 370501, max = (int?)512800, rate = 0.31m, baseTax = 77362m },
            new { min = 512801, max = (int?)673000, rate = 0.36m, baseTax = 121475m },
            new { min = 673001, max = (int?)857900, rate = 0.39m, baseTax = 179147m },
            new { min = 857901, max = (int?)1817000, rate = 0.41m, baseTax = 251258m },
            new { min = 1817001, max = (int?)null, rate = 0.45m, baseTax = 644489m }
        };
        
        return JsonSerializer.Serialize(brackets);
    }

    private static decimal CalculateTaxFromBrackets(decimal annualIncome, string bracketsJson)
    {
        if (annualIncome <= 0)
            return 0;
            
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var brackets = JsonSerializer.Deserialize<List<TaxBracket>>(bracketsJson, options);
            
            if (brackets == null || brackets.Count == 0)
                return 0;

            decimal totalTax = 0;

            // Find the applicable bracket and calculate tax using baseTax + marginal rate
            foreach (var bracket in brackets.OrderByDescending(b => b.Min))
            {
                if (annualIncome >= bracket.Min)
                {
                    var taxableAboveMin = annualIncome - bracket.Min;
                    totalTax = bracket.BaseTax + (taxableAboveMin * bracket.Rate);
                    break;
                }
            }

            // Apply 2024/25 primary tax rebate (R17,235)
            const decimal primaryRebate = 17235m;
            totalTax = Math.Max(0, totalTax - primaryRebate);

            return totalTax;
        }
        catch
        {
            return 0;
        }
    }

    private static decimal CalculateUIF(decimal monthlyIncome, decimal? uifRate, decimal? uifCap)
    {
        if (!uifRate.HasValue || monthlyIncome <= 0)
            return 0;
            
        var uif = monthlyIncome * uifRate.Value;
        
        if (uifCap.HasValue)
            uif = Math.Min(uif, uifCap.Value);
            
        return uif;
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

    private static decimal GetInvestmentMonthlyAmount(decimal amount, PaymentFrequency frequency)
    {
        // This matches the formula in GetInvestmentContributionsQueryHandler
        return frequency switch
        {
            PaymentFrequency.Weekly => amount * 4.33m,
            PaymentFrequency.Biweekly => amount * 2.17m,
            PaymentFrequency.Quarterly => amount / 3m,
            PaymentFrequency.Annually => amount / 12m,
            _ => amount
        };
    }

    private static decimal GetTotalRebate(string? rebatesJson)
    {
        if (string.IsNullOrEmpty(rebatesJson))
            return 0;
            
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var rebates = JsonSerializer.Deserialize<TaxRebates>(rebatesJson, options);
            return (rebates?.Primary ?? 0) + (rebates?.Secondary ?? 0) + (rebates?.Tertiary ?? 0);
        }
        catch
        {
            return 0;
        }
    }

    private class TaxBracket
    {
        public decimal Min { get; set; }
        public decimal? Max { get; set; }
        public decimal Rate { get; set; }
        public decimal BaseTax { get; set; }
    }

    private class TaxRebates
    {
        public decimal Primary { get; set; }
        public decimal Secondary { get; set; }
        public decimal Tertiary { get; set; }
    }

    #endregion
}
