using LifeOS.Application.Interfaces;
using LifeOS.Application.Services;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LifeOS.Tests.Services;

/// <summary>
/// Integration tests for WealthHealthCalculator.CalculateAsync()
/// Tests the full end-to-end calculation with database and finance entities
/// </summary>
public class WealthHealthCalculatorIntegrationTests : IDisposable
{
    private readonly LifeOSDbContext _context;
    private readonly IWealthHealthCalculator _calculator;
    private Guid _testUserId;

    public WealthHealthCalculatorIntegrationTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<LifeOSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new LifeOSDbContext(options);

        // Seed test data
        SeedTestData();

        // Setup service
        _calculator = new WealthHealthCalculator(_context, NullLogger<WealthHealthCalculator>.Instance);
    }

    private void SeedTestData()
    {
        // Create test user
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "test_hash",
            HomeCurrency = "USD"
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        _testUserId = user.Id;
    }

    [Fact]
    public async Task CalculateAsync_WithNoFinancialData_ReturnsLowScore()
    {
        // Act
        var result = await _calculator.CalculateAsync(_testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Score >= 0 && result.Score <= 100);
        Assert.NotEmpty(result.Components); // Will have at least diversification and emergency fund
        Assert.Contains(result.Components, c => c.ComponentCode == "diversification");
    }

    [Fact]
    public async Task CalculateAsync_WithPerfectSavingsRate_ScoresHigh()
    {
        // Arrange: 30% savings rate
        var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);
        
        _context.Transactions.AddRange(
            new Transaction
            {
                UserId = _testUserId,
                Amount = 10000m,
                Category = TransactionCategory.Income,
                RecordedAt = threeMonthsAgo.AddDays(15)
            },
            new Transaction
            {
                UserId = _testUserId,
                Amount = 7000m,
                Category = TransactionCategory.Expense,
                RecordedAt = threeMonthsAgo.AddDays(20)
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateAsync(_testUserId);

        // Assert
        var savingsComponent = result.Components.FirstOrDefault(c => c.ComponentCode == "savings_rate");
        Assert.NotNull(savingsComponent);
        Assert.Equal(100m, savingsComponent.Score); // 30% savings = perfect score
        Assert.Equal(0.30m, savingsComponent.ActualValue); // 30%
    }

    [Fact]
    public async Task CalculateAsync_WithGoodDebtToIncome_ScoresHigh()
    {
        // Arrange: 20% debt-to-income ratio (under 30% threshold)
        _context.Accounts.Add(new Account
        {
            UserId = _testUserId,
            Name = "Mortgage",
            AccountType = AccountType.Bank,
            CurrentBalance = 24000m, // $24k debt
            IsLiability = true,
            IsActive = true
        });

        _context.IncomeSources.Add(new IncomeSource
        {
            UserId = _testUserId,
            Name = "Salary",
            BaseAmount = 10000m, // $10k/month = $120k/year
            IsActive = true
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateAsync(_testUserId);

        // Assert
        var debtComponent = result.Components.FirstOrDefault(c => c.ComponentCode == "debt_to_income");
        Assert.NotNull(debtComponent);
        Assert.Equal(100m, debtComponent.Score); // 20% debt-to-income = perfect score
        Assert.Equal(0.20m, debtComponent.ActualValue);
    }

    [Fact]
    public async Task CalculateAsync_WithPoorDebtToIncome_ScoresLow()
    {
        // Arrange: 80% debt-to-income ratio (very high)
        _context.Accounts.Add(new Account
        {
            UserId = _testUserId,
            Name = "High Debt",
            AccountType = AccountType.Bank,
            CurrentBalance = 96000m, // $96k debt
            IsLiability = true,
            IsActive = true
        });

        _context.IncomeSources.Add(new IncomeSource
        {
            UserId = _testUserId,
            Name = "Salary",
            BaseAmount = 10000m, // $10k/month = $120k/year
            IsActive = true
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateAsync(_testUserId);

        // Assert
        var debtComponent = result.Components.FirstOrDefault(c => c.ComponentCode == "debt_to_income");
        Assert.NotNull(debtComponent);
        Assert.True(debtComponent.Score < 50m); // Should score poorly
    }

    [Fact]
    public async Task CalculateAsync_WithPerfectEmergencyFund_ScoresHigh()
    {
        // Arrange: 6+ months in liquid accounts (assuming $3k/month expenses = $18k)
        _context.Accounts.Add(new Account
        {
            UserId = _testUserId,
            Name = "Savings",
            AccountType = AccountType.Bank,
            CurrentBalance = 20000m,
            IsLiability = false,
            IsActive = true
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateAsync(_testUserId);

        // Assert
        var emergencyComponent = result.Components.FirstOrDefault(c => c.ComponentCode == "emergency_fund");
        Assert.NotNull(emergencyComponent);
        Assert.True(emergencyComponent.Score >= 100m); // 6+ months = perfect score
    }

    [Fact]
    public async Task CalculateAsync_WithDiversifiedAssets_ScoresHigh()
    {
        // Arrange: 4+ different account types
        _context.Accounts.AddRange(
            new Account
            {
                UserId = _testUserId,
                Name = "Checking",
                AccountType = AccountType.Bank,
                CurrentBalance = 5000m,
                IsLiability = false,
                IsActive = true
            },
            new Account
            {
                UserId = _testUserId,
                Name = "Stocks",
                AccountType = AccountType.Investment,
                CurrentBalance = 10000m,
                IsLiability = false,
                IsActive = true
            },
            new Account
            {
                UserId = _testUserId,
                Name = "Crypto",
                AccountType = AccountType.Crypto,
                CurrentBalance = 2000m,
                IsLiability = false,
                IsActive = true
            },
            new Account
            {
                UserId = _testUserId,
                Name = "Real Estate",
                AccountType = AccountType.Property,
                CurrentBalance = 200000m,
                IsLiability = false,
                IsActive = true
            }
        );

        await _context.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateAsync(_testUserId);

        // Assert
        var diversificationComponent = result.Components.FirstOrDefault(c => c.ComponentCode == "diversification");
        Assert.NotNull(diversificationComponent);
        Assert.Equal(100m, diversificationComponent.Score); // 4+ types = perfect score
    }

    [Fact]
    public async Task CalculateAsync_WithStrongNetWorthGrowth_ScoresHigh()
    {
        // Arrange: 10% growth over 12 months (above 8% target)
        var now = DateTime.UtcNow;
        var twelveMonthsAgo = now.AddMonths(-12);

        _context.NetWorthSnapshots.AddRange(
            new NetWorthSnapshot
            {
                UserId = _testUserId,
                NetWorth = 100000m,
                SnapshotDate = DateOnly.FromDateTime(twelveMonthsAgo)
            },
            new NetWorthSnapshot
            {
                UserId = _testUserId,
                NetWorth = 110000m, // 10% growth
                SnapshotDate = DateOnly.FromDateTime(now.AddDays(-1))
            }
        );

        await _context.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateAsync(_testUserId);

        // Assert
        var growthComponent = result.Components.FirstOrDefault(c => c.ComponentCode == "net_worth_growth");
        Assert.NotNull(growthComponent);
        Assert.True(growthComponent.Score >= 100m); // 10% > 8% target = capped at 100
        Assert.Equal(0.10m, growthComponent.ActualValue);
    }

    [Fact]
    public async Task CalculateAsync_WithAllPerfectComponents_ScoresNear100()
    {
        // Arrange: Perfect scores across all components
        var now = DateTime.UtcNow;
        var threeMonthsAgo = now.AddMonths(-3);
        var twelveMonthsAgo = now.AddMonths(-12);

        // Perfect savings rate (30%)
        _context.Transactions.AddRange(
            new Transaction
            {
                UserId = _testUserId,
                Amount = 10000m,
                Category = TransactionCategory.Income,
                RecordedAt = threeMonthsAgo.AddDays(15)
            },
            new Transaction
            {
                UserId = _testUserId,
                Amount = 7000m,
                Category = TransactionCategory.Expense,
                RecordedAt = threeMonthsAgo.AddDays(20)
            }
        );

        // Perfect debt-to-income (20%)
        _context.Accounts.Add(new Account
        {
            UserId = _testUserId,
            Name = "Mortgage",
            AccountType = AccountType.Bank,
            CurrentBalance = 24000m,
            IsLiability = true,
            IsActive = true
        });

        _context.IncomeSources.Add(new IncomeSource
        {
            UserId = _testUserId,
            Name = "Salary",
            BaseAmount = 10000m,
            IsActive = true
        });

        // Perfect emergency fund
        _context.Accounts.Add(new Account
        {
            UserId = _testUserId,
            Name = "Savings",
            AccountType = AccountType.Bank,
            CurrentBalance = 20000m,
            IsLiability = false,
            IsActive = true
        });

        // Perfect diversification (4+ types)
        _context.Accounts.AddRange(
            new Account
            {
                UserId = _testUserId,
                Name = "Stocks",
                AccountType = AccountType.Investment,
                CurrentBalance = 50000m,
                IsLiability = false,
                IsActive = true
            },
            new Account
            {
                UserId = _testUserId,
                Name = "Crypto",
                AccountType = AccountType.Crypto,
                CurrentBalance = 10000m,
                IsLiability = false,
                IsActive = true
            },
            new Account
            {
                UserId = _testUserId,
                Name = "Real Estate",
                AccountType = AccountType.Property,
                CurrentBalance = 300000m,
                IsLiability = false,
                IsActive = true
            }
        );

        // Perfect net worth growth (10%)
        _context.NetWorthSnapshots.AddRange(
            new NetWorthSnapshot
            {
                UserId = _testUserId,
                NetWorth = 100000m,
                SnapshotDate = DateOnly.FromDateTime(twelveMonthsAgo)
            },
            new NetWorthSnapshot
            {
                UserId = _testUserId,
                NetWorth = 110000m,
                SnapshotDate = DateOnly.FromDateTime(now.AddDays(-1))
            }
        );

        await _context.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateAsync(_testUserId);

        // Assert
        Assert.Equal(5, result.Components.Count);
        Assert.True(result.Score >= 95m); // Should be very high with all perfect components
        Assert.All(result.Components, c => Assert.True(c.Score >= 90m));
    }

    [Fact]
    public async Task CalculateAsync_WeightsAddUpCorrectly()
    {
        // Arrange: Some financial data
        _context.Accounts.Add(new Account
        {
            UserId = _testUserId,
            Name = "Checking",
            AccountType = AccountType.Bank,
            CurrentBalance = 5000m,
            IsLiability = false,
            IsActive = true
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateAsync(_testUserId);

        // Assert
        var totalWeight = result.Components.Sum(c => c.Weight);
        Assert.True(totalWeight <= 1.00m); // Should not exceed 100%
        Assert.True(totalWeight > 0m); // Should have at least diversification
    }

    [Fact]
    public async Task SaveSnapshotAsync_PersistsCorrectly()
    {
        // Arrange
        var result = await _calculator.CalculateAsync(_testUserId);

        // Act
        var snapshot = await _calculator.SaveSnapshotAsync(result, _testUserId);

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal(_testUserId, snapshot.UserId);
        Assert.Equal(result.Score, snapshot.Score);
        Assert.NotEmpty(snapshot.Components);

        // Verify it was saved to DB
        var savedSnapshot = await _context.WealthHealthSnapshots
            .FirstOrDefaultAsync(s => s.Id == snapshot.Id);
        Assert.NotNull(savedSnapshot);
        Assert.Equal(snapshot.Score, savedSnapshot.Score);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
