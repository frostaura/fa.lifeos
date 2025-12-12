using System.Text.Json;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Services;
using LifeOS.Application.Services.Mcp;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Tests.Services.Mcp;

/// <summary>
/// Integration tests for CompleteTask MCP tool handler.
/// Tests task completion, streak updates, and metric recording.
/// </summary>
public class CompleteTaskHandlerTests : IDisposable
{
    private readonly LifeOSDbContext _context;
    private readonly CompleteTaskHandler _handler;
    private readonly Guid _testUserId;

    public CompleteTaskHandlerTests()
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

        var streakService = new StreakService();
        _handler = new CompleteTaskHandler(_context, streakService);
    }

    [Fact]
    public async Task HandleAsync_WithValidTask_CreatesCompletionAndUpdatesStreak()
    {
        // Arrange
        var dimension = new Dimension { Code = "health_recovery", Name = "Health & Recovery", IsActive = true };
        _context.Dimensions.Add(dimension);
        await _context.SaveChangesAsync();
        
        var task = new LifeTask
        {
            UserId = _testUserId,
            DimensionId = dimension.Id,
            Title = "Daily exercise",
            Frequency = Frequency.Daily,
            IsActive = true
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        
        var request = new CompleteTaskRequestDto
        {
            TaskId = task.Id,
            Timestamp = new DateTime(2026, 1, 10, 19, 0, 0, DateTimeKind.Utc)
        };
        
        var jsonInput = JsonSerializer.Serialize(request);
        
        // Act
        var result = await _handler.HandleAsync(jsonInput, _testUserId, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        
        var responseDto = JsonSerializer.Deserialize<CompleteTaskResponseDto>(
            JsonSerializer.Serialize(result.Data));
        
        Assert.NotNull(responseDto);
        Assert.NotEqual(Guid.Empty, responseDto.TaskCompletionId);
        Assert.Equal(1, responseDto.UpdatedStreak.CurrentStreakLength);
        Assert.Equal(1, responseDto.UpdatedStreak.LongestStreakLength);
        Assert.Equal(0, responseDto.UpdatedStreak.RiskPenaltyScore);
        Assert.Equal(0, responseDto.UpdatedStreak.ConsecutiveMisses);
        Assert.False(responseDto.MetricRecorded);
        
        // Verify completion in database
        var completion = await _context.TaskCompletions
            .FirstOrDefaultAsync(tc => tc.TaskId == task.Id);
        
        Assert.NotNull(completion);
        Assert.Equal(CompletionType.Manual, completion.CompletionType);
        Assert.Equal(new DateTime(2026, 1, 10, 19, 0, 0, DateTimeKind.Utc), completion.CompletedAt);
        
        // Verify streak created
        var streak = await _context.Streaks
            .FirstOrDefaultAsync(s => s.TaskId == task.Id);
        
        Assert.NotNull(streak);
        Assert.Equal(1, streak.CurrentStreakLength);
    }
    
    [Fact]
    public async Task HandleAsync_WithExistingStreak_IncrementsStreak()
    {
        // Arrange
        var dimension = new Dimension { Code = "health_recovery", Name = "Health & Recovery", IsActive = true };
        _context.Dimensions.Add(dimension);
        await _context.SaveChangesAsync();
        
        var task = new LifeTask
        {
            UserId = _testUserId,
            DimensionId = dimension.Id,
            Title = "Daily exercise",
            Frequency = Frequency.Daily,
            IsActive = true
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        
        // Create existing streak
        _context.Streaks.Add(new Streak
        {
            UserId = _testUserId,
            TaskId = task.Id,
            CurrentStreakLength = 5,
            LongestStreakLength = 10,
            RiskPenaltyScore = 3,
            ConsecutiveMisses = 0,
            IsActive = true
        });
        await _context.SaveChangesAsync();
        
        var request = new CompleteTaskRequestDto
        {
            TaskId = task.Id
        };
        
        var jsonInput = JsonSerializer.Serialize(request);
        
        // Act
        var result = await _handler.HandleAsync(jsonInput, _testUserId, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success);
        
        var responseDto = JsonSerializer.Deserialize<CompleteTaskResponseDto>(
            JsonSerializer.Serialize(result.Data));
        
        Assert.NotNull(responseDto);
        Assert.Equal(6, responseDto.UpdatedStreak.CurrentStreakLength);
        Assert.Equal(10, responseDto.UpdatedStreak.LongestStreakLength);
        Assert.Equal(1, responseDto.UpdatedStreak.RiskPenaltyScore); // Decay: 3 - 2 = 1
    }
    
    [Fact]
    public async Task HandleAsync_WithMetricValue_RecordsMetric()
    {
        // Arrange
        var dimension = new Dimension { Code = "fitness_mastery", Name = "Fitness Mastery", IsActive = true };
        _context.Dimensions.Add(dimension);
        await _context.SaveChangesAsync();
        
        _context.MetricDefinitions.Add(new MetricDefinition
        {
            Code = "steps_count",
            Name = "Daily Steps",
            DimensionId = dimension.Id,
            ValueType = MetricValueType.Number,
            IsActive = true
        });
        await _context.SaveChangesAsync();
        
        var task = new LifeTask
        {
            UserId = _testUserId,
            DimensionId = dimension.Id,
            Title = "Walk 10k steps",
            LinkedMetricCode = "steps_count",
            IsActive = true
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        
        var request = new CompleteTaskRequestDto
        {
            TaskId = task.Id,
            ValueNumber = 10500
        };
        
        var jsonInput = JsonSerializer.Serialize(request);
        
        // Act
        var result = await _handler.HandleAsync(jsonInput, _testUserId, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success);
        
        var responseDto = JsonSerializer.Deserialize<CompleteTaskResponseDto>(
            JsonSerializer.Serialize(result.Data));
        
        Assert.NotNull(responseDto);
        Assert.True(responseDto.MetricRecorded);
        Assert.NotNull(responseDto.MetricRecordId);
        
        // Verify metric record in database
        var metricRecord = await _context.MetricRecords
            .FirstOrDefaultAsync(mr => mr.Id == responseDto.MetricRecordId);
        
        Assert.NotNull(metricRecord);
        Assert.Equal("steps_count", metricRecord.MetricCode);
        Assert.Equal(10500, metricRecord.ValueNumber);
        Assert.Equal("task_completion", metricRecord.Source);
    }
    
    [Fact]
    public async Task HandleAsync_AlreadyCompletedToday_ReturnsError()
    {
        // Arrange
        var dimension = new Dimension { Code = "health_recovery", Name = "Health & Recovery", IsActive = true };
        _context.Dimensions.Add(dimension);
        await _context.SaveChangesAsync();
        
        var task = new LifeTask
        {
            UserId = _testUserId,
            DimensionId = dimension.Id,
            Title = "Daily task",
            IsActive = true
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        // Create existing completion for today
        _context.TaskCompletions.Add(new TaskCompletion
        {
            TaskId = task.Id,
            UserId = _testUserId,
            CompletedAt = today.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc)
        });
        await _context.SaveChangesAsync();
        
        var request = new CompleteTaskRequestDto
        {
            TaskId = task.Id
        };
        
        var jsonInput = JsonSerializer.Serialize(request);
        
        // Act
        var result = await _handler.HandleAsync(jsonInput, _testUserId, CancellationToken.None);
        
        // Assert
        Assert.False(result.Success);
        Assert.Contains("already completed today", result.Error);
    }
    
    [Fact]
    public async Task HandleAsync_InvalidTaskId_ReturnsError()
    {
        // Arrange
        var request = new CompleteTaskRequestDto
        {
            TaskId = Guid.NewGuid()
        };
        
        var jsonInput = JsonSerializer.Serialize(request);
        
        // Act
        var result = await _handler.HandleAsync(jsonInput, _testUserId, CancellationToken.None);
        
        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.Error);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
