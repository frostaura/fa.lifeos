using System.Text.Json;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Services.Mcp;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Tests.Services.Mcp;

/// <summary>
/// Integration tests for ListTasks MCP tool handler.
/// Tests task listing with filtering and streak information.
/// </summary>
public class ListTasksHandlerTests : IDisposable
{
    private readonly LifeOSDbContext _context;
    private readonly ListTasksHandler _handler;
    private readonly Guid _testUserId;

    public ListTasksHandlerTests()
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

        _handler = new ListTasksHandler(_context);
    }

    [Fact]
    public async Task HandleAsync_WithNoFilters_ReturnsAllActiveTasks()
    {
        // Arrange
        var dimension = new Dimension { Code = "health_recovery", Name = "Health & Recovery", IsActive = true };
        _context.Dimensions.Add(dimension);
        await _context.SaveChangesAsync();
        
        var task1 = new LifeTask
        {
            UserId = _testUserId,
            DimensionId = dimension.Id,
            Title = "🏋️‍♂️ Strength training",
            TaskType = TaskType.Habit,
            Frequency = Frequency.Weekly,
            IsActive = true
        };
        
        var task2 = new LifeTask
        {
            UserId = _testUserId,
            DimensionId = dimension.Id,
            Title = "🥗 Meal prep",
            TaskType = TaskType.Habit,
            Frequency = Frequency.Daily,
            IsActive = true
        };
        
        _context.Tasks.AddRange(task1, task2);
        await _context.SaveChangesAsync();
        
        // Create streak for task1
        _context.Streaks.Add(new Streak
        {
            UserId = _testUserId,
            TaskId = task1.Id,
            CurrentStreakLength = 5,
            LongestStreakLength = 10,
            RiskPenaltyScore = 0,
            ConsecutiveMisses = 0,
            IsActive = true
        });
        await _context.SaveChangesAsync();
        
        var request = new ListTasksRequestDto
        {
            OnlyActive = true,
            IncludeCompleted = false
        };
        
        var jsonInput = JsonSerializer.Serialize(request);
        
        // Act
        var result = await _handler.HandleAsync(jsonInput, _testUserId, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        
        var responseDto = JsonSerializer.Deserialize<ListTasksResponseDto>(
            JsonSerializer.Serialize(result.Data));
        
        Assert.NotNull(responseDto);
        Assert.Equal(2, responseDto.Tasks.Count);
        
        var strengthTask = responseDto.Tasks.First(t => t.Title.Contains("Strength"));
        Assert.Equal("🏋️‍♂️", strengthTask.Emoji);
        Assert.Equal("weekly", strengthTask.Frequency);
        Assert.True(strengthTask.IsHabit);
        Assert.NotNull(strengthTask.Streak);
        Assert.Equal(5, strengthTask.Streak.CurrentStreakLength);
        Assert.Equal(10, strengthTask.Streak.LongestStreakLength);
    }
    
    [Fact]
    public async Task HandleAsync_WithDimensionFilter_ReturnsFilteredTasks()
    {
        // Arrange
        var healthDimension = new Dimension { Code = "health_recovery", Name = "Health & Recovery", IsActive = true };
        var fitnessDimension = new Dimension { Code = "fitness_mastery", Name = "Fitness Mastery", IsActive = true };
        _context.Dimensions.AddRange(healthDimension, fitnessDimension);
        await _context.SaveChangesAsync();
        
        _context.Tasks.AddRange(
            new LifeTask
            {
                UserId = _testUserId,
                DimensionId = healthDimension.Id,
                Title = "Health task",
                IsActive = true
            },
            new LifeTask
            {
                UserId = _testUserId,
                DimensionId = fitnessDimension.Id,
                Title = "Fitness task",
                IsActive = true
            });
        await _context.SaveChangesAsync();
        
        var request = new ListTasksRequestDto
        {
            DimensionCode = "health_recovery"
        };
        
        var jsonInput = JsonSerializer.Serialize(request);
        
        // Act
        var result = await _handler.HandleAsync(jsonInput, _testUserId, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success);
        
        var responseDto = JsonSerializer.Deserialize<ListTasksResponseDto>(
            JsonSerializer.Serialize(result.Data));
        
        Assert.NotNull(responseDto);
        Assert.Single(responseDto.Tasks);
        Assert.Equal("health_recovery", responseDto.Tasks[0].DimensionCode);
        Assert.Equal("Health task", responseDto.Tasks[0].Title);
    }
    
    [Fact]
    public async Task HandleAsync_WithNullInput_UsesDefaults()
    {
        // Arrange
        var dimension = new Dimension { Code = "health_recovery", Name = "Health & Recovery", IsActive = true };
        _context.Dimensions.Add(dimension);
        await _context.SaveChangesAsync();
        
        _context.Tasks.Add(new LifeTask
        {
            UserId = _testUserId,
            DimensionId = dimension.Id,
            Title = "Test task",
            IsActive = true
        });
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _handler.HandleAsync(null, _testUserId, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success);
        
        var responseDto = JsonSerializer.Deserialize<ListTasksResponseDto>(
            JsonSerializer.Serialize(result.Data));
        
        Assert.NotNull(responseDto);
        Assert.Single(responseDto.Tasks);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
