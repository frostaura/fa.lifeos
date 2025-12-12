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
/// Integration tests for HealthIndexCalculator.CalculateAsync()
/// Tests the full end-to-end calculation with database and metric aggregation
/// </summary>
public class HealthIndexCalculatorIntegrationTests : IDisposable
{
    private readonly LifeOSDbContext _context;
    private readonly IMetricAggregationService _aggregationService;
    private readonly IHealthIndexCalculator _calculator;
    private Guid _testUserId;
    private Guid _healthDimensionId;

    public HealthIndexCalculatorIntegrationTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<LifeOSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new LifeOSDbContext(options);

        // Seed test data
        SeedTestData();

        // Setup services
        _aggregationService = new MetricAggregationService(_context, NullLogger<MetricAggregationService>.Instance);
        _calculator = new HealthIndexCalculator(_context, _aggregationService, NullLogger<HealthIndexCalculator>.Instance);
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
        _context.SaveChanges(); // Save to get generated ID
        _testUserId = user.Id;

        // Create health dimension
        var dimension = new Dimension
        {
            Code = "health_recovery",
            Name = "Health & Recovery",
            IsActive = true
        };
        _context.Dimensions.Add(dimension);
        _context.SaveChanges(); // Save to get generated ID
        _healthDimensionId = dimension.Id;

        // Create metric definitions with weights
        var metrics = new[]
        {
            new MetricDefinition
            {
                Code = "weight_kg",
                Name = "Weight",
                DimensionId = _healthDimensionId,
                ValueType = MetricValueType.Number,
                AggregationType = AggregationType.Last,
                MinValue = 50,
                MaxValue = 100,
                TargetValue = 76,
                TargetDirection = TargetDirection.AtOrBelow,
                Weight = 0.20m, // 20%
                Tags = new[] { "health" },
                IsActive = true
            },
            new MetricDefinition
            {
                Code = "resting_hr",
                Name = "Resting HR",
                DimensionId = _healthDimensionId,
                ValueType = MetricValueType.Number,
                AggregationType = AggregationType.Last,
                MinValue = 40,
                MaxValue = 100,
                TargetValue = 60,
                TargetDirection = TargetDirection.AtOrBelow,
                Weight = 0.30m, // 30%
                Tags = new[] { "health" },
                IsActive = true
            },
            new MetricDefinition
            {
                Code = "hrv_ms",
                Name = "HRV",
                DimensionId = _healthDimensionId,
                ValueType = MetricValueType.Number,
                AggregationType = AggregationType.Last,
                MinValue = 20,
                MaxValue = 100,
                TargetValue = 50,
                TargetDirection = TargetDirection.AtOrAbove,
                Weight = 0.30m, // 30%
                Tags = new[] { "health" },
                IsActive = true
            },
            new MetricDefinition
            {
                Code = "body_fat_pct",
                Name = "Body Fat %",
                DimensionId = _healthDimensionId,
                ValueType = MetricValueType.Number,
                AggregationType = AggregationType.Last,
                MinValue = 13,
                MaxValue = 15,
                TargetValue = 14,
                TargetDirection = TargetDirection.Range,
                Weight = 0.20m, // 20%
                Tags = new[] { "health" },
                IsActive = true
            }
        };

        _context.MetricDefinitions.AddRange(metrics);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CalculateAsync_AllMetricsHaveData_ReturnsWeightedScore()
    {
        // Arrange: Add metric records
        var now = DateTime.UtcNow;
        _context.MetricRecords.AddRange(
            new MetricRecord { UserId = _testUserId, MetricCode = "weight_kg", ValueNumber = 76, RecordedAt = now.AddDays(-3) },      // At target = 100
            new MetricRecord { UserId = _testUserId, MetricCode = "resting_hr", ValueNumber = 60, RecordedAt = now.AddDays(-2) },     // At target = 100
            new MetricRecord { UserId = _testUserId, MetricCode = "hrv_ms", ValueNumber = 50, RecordedAt = now.AddDays(-1) },         // At target = 100
            new MetricRecord { UserId = _testUserId, MetricCode = "body_fat_pct", ValueNumber = 14, RecordedAt = now }                // In range = 100
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateAsync(_testUserId);

        // Assert
        Assert.Equal(100m, result.Score); // All at target/optimal = 100
        Assert.Equal(4, result.Components.Count);
        Assert.All(result.Components, c => Assert.Equal(100m, c.Score));
    }

    [Fact]
    public async Task CalculateAsync_SomeMetricsMissingData_SkipsGracefully()
    {
        // Arrange: Only add 2 out of 4 metrics
        var now = DateTime.UtcNow;
        _context.MetricRecords.AddRange(
            new MetricRecord { UserId = _testUserId, MetricCode = "weight_kg", ValueNumber = 76, RecordedAt = now },     // 100 score, 0.20 weight
            new MetricRecord { UserId = _testUserId, MetricCode = "resting_hr", ValueNumber = 80, RecordedAt = now }     // 50 score (midpoint), 0.30 weight
            // hrv_ms and body_fat_pct missing
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateAsync(_testUserId);

        // Assert
        Assert.Equal(2, result.Components.Count); // Only 2 metrics have data
        
        // Calculate expected weighted average: (100*0.20 + 50*0.30) / (0.20+0.30) = 35 / 0.50 = 70
        Assert.Equal(70m, result.Score);
    }

    [Fact]
    public async Task CalculateAsync_AllMetricsMissingData_ReturnsZeroScore()
    {
        // Arrange: No metric records
        // Act
        var result = await _calculator.CalculateAsync(_testUserId);

        // Assert
        Assert.Equal(0m, result.Score);
        Assert.Empty(result.Components);
    }

    [Fact]
    public async Task CalculateAsync_MixedPerformance_CalculatesCorrectWeightedAverage()
    {
        // Arrange: Mixed performance across metrics
        var now = DateTime.UtcNow;
        _context.MetricRecords.AddRange(
            // Weight: 88 kg (12 above target of 76, max=100, range=24) = (100-88)/(100-76) = 12/24 = 50% → 50 score
            new MetricRecord { UserId = _testUserId, MetricCode = "weight_kg", ValueNumber = 88, RecordedAt = now },
            
            // Resting HR: 60 (at target) = 100 score
            new MetricRecord { UserId = _testUserId, MetricCode = "resting_hr", ValueNumber = 60, RecordedAt = now },
            
            // HRV: 35 (midpoint between 20 and 50) = 50 score
            new MetricRecord { UserId = _testUserId, MetricCode = "hrv_ms", ValueNumber = 35, RecordedAt = now },
            
            // Body Fat: 14 (in range 13-15) = 100 score
            new MetricRecord { UserId = _testUserId, MetricCode = "body_fat_pct", ValueNumber = 14, RecordedAt = now }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateAsync(_testUserId);

        // Assert
        // Weighted average: (50*0.20 + 100*0.30 + 50*0.30 + 100*0.20) / 1.0
        //                 = (10 + 30 + 15 + 20) / 1.0 = 75
        Assert.Equal(75m, result.Score);
        Assert.Equal(4, result.Components.Count);
        
        var weightComponent = result.Components.First(c => c.MetricCode == "weight_kg");
        Assert.Equal(50m, weightComponent.Score);
        Assert.Equal(0.20m, weightComponent.Weight);
    }

    [Fact]
    public async Task CalculateAsync_MultipleRecordsInWindow_UsesCorrectAggregation()
    {
        // Arrange: Multiple records within 7-day window for "Last" aggregation type
        var now = DateTime.UtcNow;
        _context.MetricRecords.AddRange(
            new MetricRecord { UserId = _testUserId, MetricCode = "weight_kg", ValueNumber = 85, RecordedAt = now.AddDays(-6) },
            new MetricRecord { UserId = _testUserId, MetricCode = "weight_kg", ValueNumber = 80, RecordedAt = now.AddDays(-3) },
            new MetricRecord { UserId = _testUserId, MetricCode = "weight_kg", ValueNumber = 76, RecordedAt = now.AddDays(-1) }  // Most recent = 76
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateAsync(_testUserId);

        // Assert
        Assert.Single(result.Components);
        var component = result.Components[0];
        Assert.Equal(76m, component.ActualValue); // Should use most recent value
        Assert.Equal(100m, component.Score);      // 76 is at target
    }

    [Fact]
    public async Task CalculateAsync_DifferentLookbackPeriods_UsesCorrectWindow()
    {
        // Arrange: Records inside and outside 7-day window
        var now = DateTime.UtcNow;
        _context.MetricRecords.AddRange(
            new MetricRecord { UserId = _testUserId, MetricCode = "weight_kg", ValueNumber = 100, RecordedAt = now.AddDays(-10) }, // Outside window
            new MetricRecord { UserId = _testUserId, MetricCode = "weight_kg", ValueNumber = 76, RecordedAt = now.AddDays(-3) }    // Inside window
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateAsync(_testUserId);

        // Assert
        var component = result.Components[0];
        Assert.Equal(76m, component.ActualValue); // Should only use record within 7 days
        Assert.Equal(100m, component.Score);
    }

    [Fact]
    public async Task CalculateAsync_WeightNormalization_WorksCorrectly()
    {
        // Arrange: Weights that don't sum to 1.0
        // Update one metric to have different weight
        var metric = await _context.MetricDefinitions.FirstAsync(m => m.Code == "weight_kg");
        metric.Weight = 0.50m; // Change from 0.20 to 0.50
        await _context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        _context.MetricRecords.AddRange(
            new MetricRecord { UserId = _testUserId, MetricCode = "weight_kg", ValueNumber = 76, RecordedAt = now },     // 100 score, 0.50 weight
            new MetricRecord { UserId = _testUserId, MetricCode = "resting_hr", ValueNumber = 80, RecordedAt = now }     // 50 score, 0.30 weight
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateAsync(_testUserId);

        // Assert
        // Total weight = 0.50 + 0.30 = 0.80
        // Weighted average = (100*0.50 + 50*0.30) / 0.80 = (50 + 15) / 0.80 = 65 / 0.80 = 81.25
        Assert.Equal(81.25m, result.Score);
    }

    [Fact]
    public async Task CalculateAsync_NonHealthMetrics_NotIncluded()
    {
        // Arrange: Add a non-health metric
        var nonHealthMetric = new MetricDefinition
        {
            Code = "net_worth",
            Name = "Net Worth",
            ValueType = MetricValueType.Number,
            AggregationType = AggregationType.Last,
            TargetDirection = TargetDirection.AtOrAbove,
            Weight = 0.50m,
            Tags = new[] { "finance" }, // Not tagged as "health"
            IsActive = true
        };
        _context.MetricDefinitions.Add(nonHealthMetric);
        await _context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        _context.MetricRecords.AddRange(
            new MetricRecord { UserId = _testUserId, MetricCode = "weight_kg", ValueNumber = 76, RecordedAt = now },
            new MetricRecord { UserId = _testUserId, MetricCode = "net_worth", ValueNumber = 100000, RecordedAt = now } // Should be ignored
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateAsync(_testUserId);

        // Assert
        Assert.Single(result.Components); // Only weight_kg should be included
        Assert.DoesNotContain(result.Components, c => c.MetricCode == "net_worth");
    }

    [Fact]
    public async Task CalculateAsync_HistoricalDate_UsesCorrectTimeWindow()
    {
        // Arrange: Records at different times
        var historicalDate = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        _context.MetricRecords.AddRange(
            new MetricRecord { UserId = _testUserId, MetricCode = "weight_kg", ValueNumber = 85, RecordedAt = historicalDate.AddDays(-5) },  // Should be included
            new MetricRecord { UserId = _testUserId, MetricCode = "weight_kg", ValueNumber = 76, RecordedAt = historicalDate.AddDays(2) }    // Should NOT be included (after asOfDate)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _calculator.CalculateAsync(_testUserId, asOfDate: historicalDate);

        // Assert
        var component = result.Components[0];
        Assert.Equal(85m, component.ActualValue); // Should use historical value, not future value
    }

    [Fact]
    public async Task SaveSnapshotAsync_ValidResult_PersistsToDatabase()
    {
        // Arrange: Create a health index result
        var now = DateTime.UtcNow;
        _context.MetricRecords.Add(
            new MetricRecord { UserId = _testUserId, MetricCode = "weight_kg", ValueNumber = 76, RecordedAt = now }
        );
        await _context.SaveChangesAsync();

        var result = await _calculator.CalculateAsync(_testUserId);

        // Act
        var snapshot = await _calculator.SaveSnapshotAsync(result, _testUserId);

        // Assert
        Assert.NotEqual(Guid.Empty, snapshot.Id);
        Assert.Equal(_testUserId, snapshot.UserId);
        Assert.Equal(result.Score, snapshot.Score);
        Assert.Equal(result.CalculatedAt, snapshot.Timestamp);
        Assert.NotEqual("[]", snapshot.Components); // Should have serialized components
        
        // Verify it was persisted
        var savedSnapshot = await _context.HealthIndexSnapshots.FindAsync(snapshot.Id);
        Assert.NotNull(savedSnapshot);
        Assert.Equal(snapshot.Score, savedSnapshot.Score);
    }

    [Fact]
    public async Task SaveSnapshotAsync_MultipleSnapshots_AllPersisted()
    {
        // Arrange: Create multiple calculations
        var now = DateTime.UtcNow;
        _context.MetricRecords.AddRange(
            new MetricRecord { UserId = _testUserId, MetricCode = "weight_kg", ValueNumber = 80, RecordedAt = now.AddDays(-7) },
            new MetricRecord { UserId = _testUserId, MetricCode = "weight_kg", ValueNumber = 76, RecordedAt = now }
        );
        await _context.SaveChangesAsync();

        var result1 = await _calculator.CalculateAsync(_testUserId, now.AddDays(-7));
        var result2 = await _calculator.CalculateAsync(_testUserId, now);

        // Act
        var snapshot1 = await _calculator.SaveSnapshotAsync(result1, _testUserId);
        var snapshot2 = await _calculator.SaveSnapshotAsync(result2, _testUserId);

        // Assert
        var allSnapshots = _context.HealthIndexSnapshots
            .Where(s => s.UserId == _testUserId)
            .OrderBy(s => s.Timestamp)
            .ToList();
        
        Assert.Equal(2, allSnapshots.Count);
        Assert.NotEqual(snapshot1.Id, snapshot2.Id);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
