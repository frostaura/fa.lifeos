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
/// Integration tests for BehavioralAdherenceCalculator
/// Tests end-to-end calculation with database
/// </summary>
public class BehavioralAdherenceCalculatorIntegrationTests : IDisposable
{
    private readonly LifeOSDbContext _context;
    private readonly IBehavioralAdherenceCalculator _calculator;
    private User _testUser = null!;
    private Dimension _testDimension = null!;

    public BehavioralAdherenceCalculatorIntegrationTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<LifeOSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new LifeOSDbContext(options);
        
        var logger = NullLogger<BehavioralAdherenceCalculator>.Instance;
        _calculator = new BehavioralAdherenceCalculator(_context, logger);
        
        SeedTestData();
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
        _testUser = user;

        // Create dimension
        var dimension = new Dimension
        {
            Code = "test_dimension",
            Name = "Test",
            IsActive = true
        };
        _context.Dimensions.Add(dimension);
        _context.SaveChanges();
        _testDimension = dimension;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task CalculateAsync_NoTasks_ReturnsZeroScore()
    {
        // Act
        var result = await _calculator.CalculateAsync(_testUser.Id, DateTime.UtcNow, 7);

        // Assert
        Assert.Equal(0m, result.Score);
        Assert.Equal(0m, result.RawAdherence);
        Assert.Equal(1m, result.PenaltyFactor); // No streaks = no penalty
        Assert.Equal(0, result.TasksScheduled);
        Assert.Equal(0, result.TasksCompleted);
    }

    [Fact]
    public async Task CalculateAsync_100PercentCompletion_NoPenalties_Returns100()
    {
        // Arrange - Create a daily habit task
        var task = new LifeTask
        {
            UserId = _testUser.Id,
            DimensionId = _testDimension.Id,
            Title = "Daily Exercise",
            TaskType = TaskType.Habit,
            Frequency = Frequency.Daily,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            IsActive = true
        };
        _context.Tasks.Add(task);
        _context.SaveChanges();

        // Create 7 task completions (one per day for 7 days)
        var evaluationDate = DateTime.UtcNow;
        for (int i = 0; i < 7; i++)
        {
            var completion = new TaskCompletion
            {
                TaskId = task.Id,
                UserId = _testUser.Id,
                CompletedAt = evaluationDate.AddDays(-i).AddHours(-1),
                CompletionType = CompletionType.Manual
            };
            _context.TaskCompletions.Add(completion);
        }
        _context.SaveChanges();

        // Act
        var result = await _calculator.CalculateAsync(_testUser.Id, evaluationDate, 7);

        // Assert
        Assert.Equal(100m, result.Score);
        Assert.Equal(1m, result.RawAdherence); // 7/7 = 1.0
        Assert.Equal(1m, result.PenaltyFactor); // No penalty
        Assert.Equal(7, result.TasksScheduled); // 7 days × 1 daily task
        Assert.Equal(7, result.TasksCompleted);
    }

    [Fact]
    public async Task CalculateAsync_50PercentCompletion_NoPenalties_ReturnsCorrectScore()
    {
        // Arrange
        var task = new LifeTask
        {
            UserId = _testUser.Id,
            DimensionId = _testDimension.Id,
            Title = "Daily Meditation",
            TaskType = TaskType.Habit,
            Frequency = Frequency.Daily,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            IsActive = true
        };
        _context.Tasks.Add(task);
        _context.SaveChanges();

        // Create only 3 completions out of 7 days
        var evaluationDate = DateTime.UtcNow;
        for (int i = 0; i < 3; i++)
        {
            _context.TaskCompletions.Add(new TaskCompletion
            {
                TaskId = task.Id,
                UserId = _testUser.Id,
                CompletedAt = evaluationDate.AddDays(-i).AddHours(-1)
            });
        }
        _context.SaveChanges();

        // Act
        var result = await _calculator.CalculateAsync(_testUser.Id, evaluationDate, 7);

        // Assert
        Assert.Equal(7, result.TasksScheduled);
        Assert.Equal(3, result.TasksCompleted);
        Assert.Equal(3m / 7m, result.RawAdherence);
        Assert.Equal(1m, result.PenaltyFactor); // No streaks = no penalty
        Assert.Equal((3m / 7m) * 100m, result.Score); // ~42.86
    }

    [Fact]
    public async Task CalculateAsync_100PercentCompletion_WithHighPenalties_ReducesScore()
    {
        // Arrange
        var task = new LifeTask
        {
            UserId = _testUser.Id,
            DimensionId = _testDimension.Id,
            Title = "Daily Exercise",
            TaskType = TaskType.Habit,
            Frequency = Frequency.Daily,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            IsActive = true
        };
        _context.Tasks.Add(task);
        _context.SaveChanges();

        // Complete all tasks
        var evaluationDate = DateTime.UtcNow;
        for (int i = 0; i < 7; i++)
        {
            _context.TaskCompletions.Add(new TaskCompletion
            {
                TaskId = task.Id,
                UserId = _testUser.Id,
                CompletedAt = evaluationDate.AddDays(-i).AddHours(-1)
            });
        }

        // Add streak with high penalty (e.g., 50 penalty points)
        var streak = new Streak
        {
            UserId = _testUser.Id,
            TaskId = task.Id,
            RiskPenaltyScore = 50m, // High penalty
            ConsecutiveMisses = 6,
            IsActive = true
        };
        _context.Streaks.Add(streak);
        _context.SaveChanges();

        // Act
        var result = await _calculator.CalculateAsync(_testUser.Id, evaluationDate, 7);

        // Assert
        Assert.Equal(1m, result.RawAdherence); // 100% completion
        // avgPenalty = 50 / 100 = 0.5
        // penaltyFactor = clamp(1 - 0.5, 0.5, 1.0) = 0.5
        Assert.Equal(0.5m, result.PenaltyFactor);
        // score = 1.0 × 100 × 0.5 = 50
        Assert.Equal(50m, result.Score);
    }

    [Fact]
    public async Task CalculateAsync_PenaltyFactor_NeverGoesBelowHalf()
    {
        // Arrange
        var task = new LifeTask
        {
            UserId = _testUser.Id,
            DimensionId = _testDimension.Id,
            Title = "Daily Task",
            TaskType = TaskType.Habit,
            Frequency = Frequency.Daily,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            IsActive = true
        };
        _context.Tasks.Add(task);
        _context.SaveChanges();

        for (int i = 0; i < 7; i++)
        {
            _context.TaskCompletions.Add(new TaskCompletion
            {
                TaskId = task.Id,
                UserId = _testUser.Id,
                CompletedAt = DateTime.UtcNow.AddDays(-i)
            });
        }

        // Add streak with extreme penalty (should be clamped)
        var streak = new Streak
        {
            UserId = _testUser.Id,
            TaskId = task.Id,
            RiskPenaltyScore = 200m, // Extreme penalty (> 100)
            ConsecutiveMisses = 20,
            IsActive = true
        };
        _context.Streaks.Add(streak);
        _context.SaveChanges();

        // Act
        var result = await _calculator.CalculateAsync(_testUser.Id, DateTime.UtcNow, 7);

        // Assert
        // avgPenalty = 200 / 100 = 2.0
        // penaltyFactor = clamp(1 - 2.0, 0.5, 1.0) = 0.5 (clamped)
        Assert.Equal(0.5m, result.PenaltyFactor);
        // score = 1.0 × 100 × 0.5 = 50
        Assert.Equal(50m, result.Score);
    }

    [Fact]
    public async Task CalculateAsync_MultipleStreaks_AveragesPenalties()
    {
        // Arrange
        var task1 = new LifeTask
        {
            UserId = _testUser.Id,
            DimensionId = _testDimension.Id,
            Title = "Task 1",
            TaskType = TaskType.Habit,
            Frequency = Frequency.Daily,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            IsActive = true
        };
        var task2 = new LifeTask
        {
            UserId = _testUser.Id,
            DimensionId = _testDimension.Id,
            Title = "Task 2",
            TaskType = TaskType.Habit,
            Frequency = Frequency.Daily,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            IsActive = true
        };
        _context.Tasks.Add(task1);
        _context.Tasks.Add(task2);
        _context.SaveChanges();

        // Complete all instances (14 total: 7 days × 2 tasks)
        for (int i = 0; i < 7; i++)
        {
            _context.TaskCompletions.Add(new TaskCompletion
            {
                TaskId = task1.Id,
                UserId = _testUser.Id,
                CompletedAt = DateTime.UtcNow.AddDays(-i)
            });
            _context.TaskCompletions.Add(new TaskCompletion
            {
                TaskId = task2.Id,
                UserId = _testUser.Id,
                CompletedAt = DateTime.UtcNow.AddDays(-i)
            });
        }

        // Add two streaks with different penalties
        _context.Streaks.Add(new Streak
        {
            UserId = _testUser.Id,
            TaskId = task1.Id,
            RiskPenaltyScore = 20m,
            IsActive = true
        });
        _context.Streaks.Add(new Streak
        {
            UserId = _testUser.Id,
            TaskId = task2.Id,
            RiskPenaltyScore = 40m,
            IsActive = true
        });
        _context.SaveChanges();

        // Act
        var result = await _calculator.CalculateAsync(_testUser.Id, DateTime.UtcNow, 7);

        // Assert
        // avgPenalty = ((20 + 40) / 2) / 100 = 0.3
        // penaltyFactor = clamp(1 - 0.3, 0.5, 1.0) = 0.7
        Assert.Equal(0.7m, result.PenaltyFactor);
        Assert.Equal(14, result.TasksScheduled); // 7 days × 2 tasks
        Assert.Equal(14, result.TasksCompleted);
        Assert.Equal(1m, result.RawAdherence);
        // score = 1.0 × 100 × 0.7 = 70
        Assert.Equal(70m, result.Score);
    }

    [Fact]
    public async Task SaveSnapshotAsync_PersistsToDatabase()
    {
        // Arrange
        var result = new AdherenceResult
        {
            Score = 75m,
            RawAdherence = 0.85m,
            PenaltyFactor = 0.882m,
            TimeWindowDays = 7,
            TasksScheduled = 10,
            TasksCompleted = 8,
            CalculatedAt = DateTime.UtcNow
        };

        // Act
        var snapshot = await _calculator.SaveSnapshotAsync(result, _testUser.Id);

        // Assert
        Assert.NotEqual(Guid.Empty, snapshot.Id);
        Assert.Equal(_testUser.Id, snapshot.UserId);
        Assert.Equal(75m, snapshot.Score);
        Assert.Equal(0.85m, snapshot.RawAdherence);
        Assert.Equal(0.882m, snapshot.PenaltyFactor);
        Assert.Equal(7, snapshot.TimeWindowDays);
        Assert.Equal(10, snapshot.TasksConsidered);
        Assert.Equal(8, snapshot.TasksCompleted);

        // Verify it's in the database
        var retrieved = await _context.AdherenceSnapshots
            .FirstOrDefaultAsync(s => s.Id == snapshot.Id);
        
        Assert.NotNull(retrieved);
        Assert.Equal(snapshot.Score, retrieved.Score);
    }

    [Fact]
    public async Task CalculateAsync_WeeklyTasks_SchedulesCorrectly()
    {
        // Arrange
        var weeklyTask = new LifeTask
        {
            UserId = _testUser.Id,
            DimensionId = _testDimension.Id,
            Title = "Weekly Review",
            TaskType = TaskType.Habit,
            Frequency = Frequency.Weekly,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            IsActive = true
        };
        _context.Tasks.Add(weeklyTask);
        _context.SaveChanges();

        // Complete the weekly task once
        _context.TaskCompletions.Add(new TaskCompletion
        {
            TaskId = weeklyTask.Id,
            UserId = _testUser.Id,
            CompletedAt = DateTime.UtcNow.AddDays(-3)
        });
        _context.SaveChanges();

        // Act
        var result = await _calculator.CalculateAsync(_testUser.Id, DateTime.UtcNow, 7);

        // Assert
        Assert.Equal(1, result.TasksScheduled); // 7 days / 7 = 1 weekly task
        Assert.Equal(1, result.TasksCompleted);
        Assert.Equal(100m, result.Score); // 100% completion
    }
}
