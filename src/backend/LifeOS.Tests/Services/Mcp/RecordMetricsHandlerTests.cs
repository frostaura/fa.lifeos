using System.Text.Json;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Interfaces;
using LifeOS.Application.Services;
using LifeOS.Application.Services.Mcp;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LifeOS.Tests.Services.Mcp;

/// <summary>
/// Integration tests for RecordMetrics MCP tool handler.
/// Tests metric recording through MCP interface with nested structures.
/// </summary>
public class RecordMetricsHandlerTests : IDisposable
{
    private readonly LifeOSDbContext _context;
    private readonly RecordMetricsHandler _handler;
    private readonly Guid _testUserId;

    public RecordMetricsHandlerTests()
    {
        var options = new DbContextOptionsBuilder<LifeOSDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new LifeOSDbContext(options);

        // Create test user
        var user = new User
        {
            Email = "test@lifeos.com",
            Username = "testuser",
            PasswordHash = "test_hash",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        
        _testUserId = user.Id;

        // Setup service
        var metricIngestionService = new MetricIngestionService(_context, NullLogger<MetricIngestionService>.Instance);
        _handler = new RecordMetricsHandler(metricIngestionService);
    }

    [Fact]
    public async Task HandleAsync_WithFlatMetrics_CreatesMetricRecords()
    {
        // Arrange
        var dimension = new Dimension { Code = "health_recovery", Name = "Health & Recovery", IsActive = true };
        _context.Dimensions.Add(dimension);
        
        _context.MetricDefinitions.AddRange(
            new MetricDefinition
            {
                Code = "weight_kg",
                Name = "Weight (kg)",
                DimensionId = dimension.Id,
                ValueType = MetricValueType.Number,
                MinValue = 30,
                MaxValue = 300,
                IsActive = true
            },
            new MetricDefinition
            {
                Code = "steps_count",
                Name = "Daily Steps",
                DimensionId = dimension.Id,
                ValueType = MetricValueType.Number,
                MinValue = 0,
                IsActive = true
            });
        
        await _context.SaveChangesAsync();
        
        var request = new RecordMetricsRequestDto
        {
            Timestamp = new DateTime(2026, 1, 10, 8, 30, 0, DateTimeKind.Utc),
            Source = "ai_assistant",
            Metrics = new Dictionary<string, object>
            {
                { "weight_kg", 82.5 },
                { "steps_count", 10432 }
            }
        };
        
        var jsonInput = JsonSerializer.Serialize(request);
        
        // Act
        var result = await _handler.HandleAsync(jsonInput, _testUserId, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        
        var responseDto = JsonSerializer.Deserialize<RecordMetricsResponseDto>(
            JsonSerializer.Serialize(result.Data));
        
        Assert.NotNull(responseDto);
        Assert.Equal(2, responseDto.CreatedRecords);
        Assert.Empty(responseDto.IgnoredMetrics);
        Assert.Empty(responseDto.Errors);
        
        // Verify records in database
        var weightRecord = await _context.MetricRecords
            .FirstOrDefaultAsync(m => m.UserId == _testUserId && m.MetricCode == "weight_kg");
        Assert.NotNull(weightRecord);
        Assert.Equal(82.5m, weightRecord.ValueNumber);
        
        var stepsRecord = await _context.MetricRecords
            .FirstOrDefaultAsync(m => m.UserId == _testUserId && m.MetricCode == "steps_count");
        Assert.NotNull(stepsRecord);
        Assert.Equal(10432m, stepsRecord.ValueNumber);
    }
    
    [Fact]
    public async Task HandleAsync_WithInvalidMetricCode_ReturnsPartialSuccess()
    {
        // Arrange
        var dimension = new Dimension { Code = "health_recovery", Name = "Health & Recovery", IsActive = true };
        _context.Dimensions.Add(dimension);
        
        _context.MetricDefinitions.Add(new MetricDefinition
        {
            Code = "weight_kg",
            Name = "Weight (kg)",
            DimensionId = dimension.Id,
            ValueType = MetricValueType.Number,
            IsActive = true
        });
        
        await _context.SaveChangesAsync();
        
        var request = new RecordMetricsRequestDto
        {
            Metrics = new Dictionary<string, object>
            {
                { "weight_kg", 80.0 },
                { "invalid_metric", 123 }
            }
        };
        
        var jsonInput = JsonSerializer.Serialize(request);
        
        // Act
        var result = await _handler.HandleAsync(jsonInput, _testUserId, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success); // Partial success
        
        var responseDto = JsonSerializer.Deserialize<RecordMetricsResponseDto>(
            JsonSerializer.Serialize(result.Data));
        
        Assert.NotNull(responseDto);
        Assert.Equal(1, responseDto.CreatedRecords);
        Assert.Contains(responseDto.Errors, e => e.Contains("invalid_metric"));
    }
    
    [Fact]
    public async Task HandleAsync_WithNullMetrics_ReturnsError()
    {
        // Arrange
        var request = new RecordMetricsRequestDto
        {
            Metrics = new Dictionary<string, object>()
        };
        
        var jsonInput = JsonSerializer.Serialize(request);
        
        // Act
        var result = await _handler.HandleAsync(jsonInput, _testUserId, CancellationToken.None);
        
        // Assert
        Assert.False(result.Success);
        Assert.Contains("No metrics provided", result.Error);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
