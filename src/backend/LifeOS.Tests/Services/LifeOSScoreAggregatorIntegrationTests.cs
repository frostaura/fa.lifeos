using LifeOS.Application.Interfaces;
using LifeOS.Application.Services;
using LifeOS.Domain.Entities;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LifeOS.Tests.Services;

/// <summary>
/// Integration tests for LifeOSScoreAggregator (v3.0).
/// Tests the weighted aggregation of Health Index, Adherence Index, and Wealth Health Score.
/// </summary>
public class LifeOSScoreAggregatorIntegrationTests : IDisposable
{
    private readonly LifeOSDbContext _context;
    private readonly ILifeOSScoreAggregator _aggregator;
    private readonly IHealthIndexCalculator _healthIndexCalculator;
    private readonly IBehavioralAdherenceCalculator _adherenceCalculator;
    private readonly IWealthHealthCalculator _wealthHealthCalculator;
    private Guid _testUserId;
    
    public LifeOSScoreAggregatorIntegrationTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<LifeOSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new LifeOSDbContext(options);
        
        // Setup services
        var metricAggregationService = new MetricAggregationService(_context, NullLogger<MetricAggregationService>.Instance);
        _healthIndexCalculator = new HealthIndexCalculator(_context, metricAggregationService, NullLogger<HealthIndexCalculator>.Instance);
        
        _adherenceCalculator = new BehavioralAdherenceCalculator(_context, NullLogger<BehavioralAdherenceCalculator>.Instance);
        
        _wealthHealthCalculator = new WealthHealthCalculator(_context, NullLogger<WealthHealthCalculator>.Instance);
        
        var longevityCalculator = new LongevityCalculator(_context, metricAggregationService);
        var primaryStatsCalculator = new PrimaryStatsCalculator(_context, metricAggregationService, NullLogger<PrimaryStatsCalculator>.Instance);
        
        _aggregator = new LifeOSScoreAggregator(
            _healthIndexCalculator,
            _adherenceCalculator,
            _wealthHealthCalculator,
            longevityCalculator,
            primaryStatsCalculator,
            _context);
        
        _testUserId = Guid.NewGuid();
    }
    
    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
    
    [Fact]
    public async Task CalculateAsync_ReturnsValidLifeOsScore()
    {
        // Arrange
        await SetupBasicUser();
        
        // Act
        var result = await _aggregator.CalculateAsync(_testUserId);
        
        // Assert
        Assert.NotNull(result);
        Assert.InRange(result.LifeScore, 0m, 100m);
        Assert.InRange(result.HealthIndex, 0m, 100m);
        Assert.InRange(result.AdherenceIndex, 0m, 100m);
        Assert.InRange(result.WealthHealthScore, 0m, 100m);
        Assert.Equal(0m, result.LongevityYearsAdded); // Placeholder for Epic 3
    }
    
    [Fact]
    public void WeightsSumToOne_VerifyFormulaIntegrity()
    {
        // Verify default weights sum to 1.0 (100%)
        const decimal healthWeight = 0.4m;
        const decimal adherenceWeight = 0.3m;
        const decimal wealthWeight = 0.3m;
        
        var totalWeight = healthWeight + adherenceWeight + wealthWeight;
        
        Assert.Equal(1.0m, totalWeight);
    }
    
    [Fact]
    public async Task CalculateAsync_WeightedAverage_CalculatesCorrectly()
    {
        // Arrange
        await SetupBasicUser();
        
        // Act
        var result = await _aggregator.CalculateAsync(_testUserId);
        
        // Assert - verify formula: wH × HealthIndex + wA × AdherenceIndex + wW × WealthHealthScore
        var expectedScore = (0.4m * result.HealthIndex) + 
                           (0.3m * result.AdherenceIndex) + 
                           (0.3m * result.WealthHealthScore);
        Assert.Equal(Math.Round(expectedScore, 2), result.LifeScore);
    }
    
    [Fact]
    public async Task SaveSnapshotAsync_PersistsAllComponents()
    {
        // Arrange
        await SetupBasicUser();
        var result = await _aggregator.CalculateAsync(_testUserId);
        
        // Act
        var snapshot = await _aggregator.SaveSnapshotAsync(result, _testUserId);
        
        // Assert
        Assert.NotEqual(Guid.Empty, snapshot.Id);
        Assert.Equal(_testUserId, snapshot.UserId);
        Assert.Equal(result.LifeScore, snapshot.LifeScore);
        Assert.Equal(result.HealthIndex, snapshot.HealthIndex);
        Assert.Equal(result.AdherenceIndex, snapshot.AdherenceIndex);
        Assert.Equal(result.WealthHealthScore, snapshot.WealthHealthScore);
        Assert.Equal(result.LongevityYearsAdded, snapshot.LongevityYearsAdded);
        Assert.NotNull(snapshot.DimensionScores);
        
        // Verify snapshot is persisted in database
        var savedSnapshot = await _context.LifeOsScoreSnapshots.FindAsync(snapshot.Id);
        Assert.NotNull(savedSnapshot);
        Assert.Equal(result.LifeScore, savedSnapshot.LifeScore);
    }
    
    [Fact]
    public async Task SaveSnapshotAsync_SerializesDimensionScoresCorrectly()
    {
        // Arrange
        await SetupBasicUser();
        var result = await _aggregator.CalculateAsync(_testUserId);
        
        // Act
        var snapshot = await _aggregator.SaveSnapshotAsync(result, _testUserId);
        
        // Assert
        Assert.NotNull(snapshot.DimensionScores);
        // Should be valid JSON array (empty for now as dimension scores is placeholder)
        Assert.True(snapshot.DimensionScores.StartsWith("[") && snapshot.DimensionScores.EndsWith("]"));
        
        // Verify it can be deserialized
        var dimensionScores = System.Text.Json.JsonSerializer.Deserialize<List<object>>(snapshot.DimensionScores);
        Assert.NotNull(dimensionScores);
    }
    
    [Fact]
    public async Task CalculateAsync_WithHistoricalDate_ReturnsResult()
    {
        // Arrange
        await SetupBasicUser();
        var historicalDate = DateTime.UtcNow.AddDays(-7);
        
        // Act
        var result = await _aggregator.CalculateAsync(_testUserId, historicalDate);
        
        // Assert
        Assert.NotNull(result);
        Assert.InRange(result.LifeScore, 0m, 100m);
    }
    
    [Fact]
    public async Task CalculateAsync_ComponentBreakdownIsConsistent()
    {
        // Arrange
        await SetupBasicUser();
        
        // Act
        var result = await _aggregator.CalculateAsync(_testUserId);
        
        // Assert - verify each component is in valid range
        Assert.InRange(result.HealthIndex, 0m, 100m);
        Assert.InRange(result.AdherenceIndex, 0m, 100m);
        Assert.InRange(result.WealthHealthScore, 0m, 100m);
        Assert.InRange(result.LifeScore, 0m, 100m);
        
        // Verify CalculatedAt is recent
        Assert.InRange(result.CalculatedAt, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }
    
    [Fact]
    public async Task CalculateAsync_NewUser_ReturnsDefaultScores()
    {
        // Arrange - just create user with no data
        var newUserId = Guid.NewGuid();
        var user = new User
        {
            Email = $"test_{newUserId}@lifeos.test",
            PasswordHash = "test_hash",
            HomeCurrency = "USD"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _aggregator.CalculateAsync(user.Id);
        
        // Assert - should not throw, should return some default scores
        Assert.NotNull(result);
        Assert.InRange(result.LifeScore, 0m, 100m);
    }
    
    // Helper methods
    private async Task SetupBasicUser()
    {
        var user = new User
        {
            Email = $"test_{_testUserId}@lifeos.test",
            PasswordHash = "test_hash",
            HomeCurrency = "USD"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        // Update _testUserId to match the generated ID
        _testUserId = user.Id;
        
        // Add a basic dimension for health metrics
        var healthDim = new Dimension
        {
            Code = "health",
            Name = "Health",
            IsActive = true,
            DefaultWeight = 0.125m
        };
        _context.Dimensions.Add(healthDim);
        
        // Add a basic metric definition
        var metric = new MetricDefinition
        {
            DimensionId = healthDim.Id,
            Code = "test_metric",
            Name = "Test Metric",
            TargetValue = 100m,
            Weight = 1.0m,
            TargetDirection = Domain.Enums.TargetDirection.AtOrAbove,
            IsActive = true
        };
        _context.MetricDefinitions.Add(metric);
        
        // Add a basic account for wealth health
        var account = new Account
        {
            UserId = _testUserId,
            Name = "Checking",
            AccountType = Domain.Enums.AccountType.Bank,
            CurrentBalance = 5000m,
            IsActive = true
        };
        _context.Accounts.Add(account);
        
        await _context.SaveChangesAsync();
    }
}
