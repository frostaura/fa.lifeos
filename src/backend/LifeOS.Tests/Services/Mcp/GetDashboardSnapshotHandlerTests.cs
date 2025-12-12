using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Interfaces;
using LifeOS.Application.Services.Mcp;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace LifeOS.Tests.Services.Mcp;

/// <summary>
/// Integration tests for GetDashboardSnapshotHandler MCP tool.
/// Tests comprehensive dashboard snapshot retrieval with all required fields.
/// </summary>
public class GetDashboardSnapshotHandlerTests : IDisposable
{
    private readonly LifeOSDbContext _context;
    private readonly GetDashboardSnapshotHandler _handler;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Mock<ILifeOSScoreAggregator> _mockScoreAggregator;
    private readonly Mock<IPrimaryStatsCalculator> _mockStatsCalculator;

    public GetDashboardSnapshotHandlerTests()
    {
        var options = new DbContextOptionsBuilder<LifeOSDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new LifeOSDbContext(options);

        _mockScoreAggregator = new Mock<ILifeOSScoreAggregator>();
        _mockStatsCalculator = new Mock<IPrimaryStatsCalculator>();

        _handler = new GetDashboardSnapshotHandler(
            _mockScoreAggregator.Object,
            _mockStatsCalculator.Object,
            _context);

        SeedDatabase();
        
        // Get the userId from the created user
        _testUserId = _context.Users.First().Id;
    }

    private void SeedDatabase()
    {
        // Create user (Id auto-generated)
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
        
        // Use the auto-generated ID
        var userId = user.Id;

        // Create dimensions
        var healthDimension = new Dimension
        {
            Code = "health_recovery",
            Name = "Health & Recovery",
            IsActive = true
        };
        var relationshipsDimension = new Dimension
        {
            Code = "relationships",
            Name = "Relationships",
            IsActive = true
        };
        _context.Dimensions.AddRange(healthDimension, relationshipsDimension);
        _context.SaveChanges();

        // Create tasks
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        var dailyTask1 = new LifeTask
        {
            UserId = userId,
            DimensionId = healthDimension.Id,
            Title = "🚶‍♂️ 10k steps",
            Frequency = Frequency.Daily,
            IsActive = true,
            StartDate = today.AddDays(-7)
        };
        
        var dailyTask2 = new LifeTask
        {
            UserId = userId,
            DimensionId = relationshipsDimension.Id,
            Title = "📞 Call a friend",
            Frequency = Frequency.Daily,
            IsActive = true,
            StartDate = today.AddDays(-7)
        };
        
        var scheduledTask = new LifeTask
        {
            UserId = userId,
            DimensionId = healthDimension.Id,
            Title = "🏋️ Gym session",
            Frequency = Frequency.Weekly,
            ScheduledDate = today,
            IsActive = true,
            StartDate = today.AddDays(-7)
        };

        _context.Tasks.AddRange(dailyTask1, dailyTask2, scheduledTask);
        _context.SaveChanges();

        // Create task completion for dailyTask1
        var completion = new TaskCompletion
        {
            TaskId = dailyTask1.Id,
            UserId = userId,
            CompletedAt = DateTime.UtcNow,
            CompletionType = CompletionType.Manual
        };
        _context.TaskCompletions.Add(completion);

        // Create net worth metric
        var netWorthMetric = new MetricRecord
        {
            UserId = userId,
            MetricCode = "net_worth_homeccy",
            ValueNumber = 1200000m,
            RecordedAt = DateTime.UtcNow.AddDays(-1),
            Source = "manual"
        };
        _context.MetricRecords.Add(netWorthMetric);

        // Create weekly review metric (8 days ago - due)
        var weeklyReviewMetric = new MetricRecord
        {
            UserId = userId,
            MetricCode = "system.weekly_review_done",
            ValueBoolean = true,
            RecordedAt = DateTime.UtcNow.AddDays(-8),
            Source = "system"
        };
        _context.MetricRecords.Add(weeklyReviewMetric);

        _context.SaveChanges();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_WithCompleteSnapshot()
    {
        // Arrange
        SetupMockResponses();

        // Act
        var result = await _handler.HandleAsync(null, _testUserId, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);

        var snapshot = result.Data as DashboardSnapshotDto;
        Assert.NotNull(snapshot);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAllScoreComponents()
    {
        // Arrange
        SetupMockResponses();

        // Act
        var result = await _handler.HandleAsync(null, _testUserId, CancellationToken.None);

        // Assert
        var snapshot = result.Data as DashboardSnapshotDto;
        Assert.NotNull(snapshot);
        
        Assert.Equal(82m, snapshot.LifeScore);
        Assert.Equal(78m, snapshot.HealthIndex);
        Assert.Equal(85m, snapshot.AdherenceIndex);
        Assert.Equal(80m, snapshot.WealthHealthScore);
        Assert.Equal(16.5m, snapshot.LongevityYearsAdded);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnPrimaryStats()
    {
        // Arrange
        SetupMockResponses();

        // Act
        var result = await _handler.HandleAsync(null, _testUserId, CancellationToken.None);

        // Assert
        var snapshot = result.Data as DashboardSnapshotDto;
        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot.PrimaryStats);
        Assert.Equal(7, snapshot.PrimaryStats.Count);
        
        Assert.Equal(72, snapshot.PrimaryStats["strength"]);
        Assert.Equal(88, snapshot.PrimaryStats["wisdom"]);
        Assert.Equal(81, snapshot.PrimaryStats["charisma"]);
        Assert.Equal(79, snapshot.PrimaryStats["composure"]);
        Assert.Equal(76, snapshot.PrimaryStats["energy"]);
        Assert.Equal(74, snapshot.PrimaryStats["influence"]);
        Assert.Equal(83, snapshot.PrimaryStats["vitality"]);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnDimensionScores()
    {
        // Arrange
        SetupMockResponses();

        // Act
        var result = await _handler.HandleAsync(null, _testUserId, CancellationToken.None);

        // Assert
        var snapshot = result.Data as DashboardSnapshotDto;
        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot.Dimensions);
        Assert.Equal(8, snapshot.Dimensions.Count);
        
        var healthDim = snapshot.Dimensions.FirstOrDefault(d => d.Code == "health_recovery");
        Assert.NotNull(healthDim);
        Assert.Equal(80m, healthDim.Score);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTodayTasks()
    {
        // Arrange
        SetupMockResponses();

        // Act
        var result = await _handler.HandleAsync(null, _testUserId, CancellationToken.None);

        // Assert
        var snapshot = result.Data as DashboardSnapshotDto;
        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot.TodayTasks);
        Assert.Equal(3, snapshot.TodayTasks.Count);
        
        // Check daily task 1 is marked as completed
        var completedTask = snapshot.TodayTasks.FirstOrDefault(t => t.Title == "🚶‍♂️ 10k steps");
        Assert.NotNull(completedTask);
        Assert.True(completedTask.IsCompleted);
        Assert.Equal("health_recovery", completedTask.DimensionCode);
        Assert.Equal("daily", completedTask.Frequency);
        
        // Check daily task 2 is not completed
        var uncompletedTask = snapshot.TodayTasks.FirstOrDefault(t => t.Title == "📞 Call a friend");
        Assert.NotNull(uncompletedTask);
        Assert.False(uncompletedTask.IsCompleted);
        
        // Check scheduled task
        var scheduledTask = snapshot.TodayTasks.FirstOrDefault(t => t.Title == "🏋️ Gym session");
        Assert.NotNull(scheduledTask);
        Assert.Equal("weekly", scheduledTask.Frequency);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNetWorth()
    {
        // Arrange
        SetupMockResponses();

        // Act
        var result = await _handler.HandleAsync(null, _testUserId, CancellationToken.None);

        // Assert
        var snapshot = result.Data as DashboardSnapshotDto;
        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot.NetWorthHomeCcy);
        Assert.Equal(1200000m, snapshot.NetWorthHomeCcy.Value);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnWeeklyReviewDue()
    {
        // Arrange
        SetupMockResponses();

        // Act
        var result = await _handler.HandleAsync(null, _testUserId, CancellationToken.None);

        // Assert
        var snapshot = result.Data as DashboardSnapshotDto;
        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot.NextKeyEvents);
        Assert.NotEmpty(snapshot.NextKeyEvents);
        
        var reviewEvent = snapshot.NextKeyEvents.FirstOrDefault(e => e.Type == "weekly_review_due");
        Assert.NotNull(reviewEvent);
        Assert.NotEmpty(reviewEvent.Date);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", reviewEvent.Date); // ISO 8601 format
    }

    [Fact]
    public async Task HandleAsync_ShouldNotReturnWeeklyReviewDue_WhenReviewRecent()
    {
        // Arrange
        SetupMockResponses();
        
        // Update review to be recent (2 days ago)
        var recentReview = _context.MetricRecords
            .First(m => m.MetricCode == "system.weekly_review_done");
        recentReview.RecordedAt = DateTime.UtcNow.AddDays(-2);
        _context.SaveChanges();

        // Act
        var result = await _handler.HandleAsync(null, _testUserId, CancellationToken.None);

        // Assert
        var snapshot = result.Data as DashboardSnapshotDto;
        Assert.NotNull(snapshot);
        Assert.Empty(snapshot.NextKeyEvents);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnToolNameAndDescription()
    {
        // Assert
        Assert.Equal("lifeos.getDashboardSnapshot", _handler.ToolName);
        Assert.NotEmpty(_handler.Description);
        Assert.Contains("dashboard", _handler.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleUserWithNoTasks()
    {
        // Arrange
        var emptyUser = new User
        {
            Email = "empty@lifeos.com",
            Username = "emptyuser",
            PasswordHash = "test_hash",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(emptyUser);
        _context.SaveChanges();
        
        var emptyUserId = emptyUser.Id;
        
        SetupMockResponses();

        // Act
        var result = await _handler.HandleAsync(null, emptyUserId, CancellationToken.None);

        // Assert
        var snapshot = result.Data as DashboardSnapshotDto;
        Assert.NotNull(snapshot);
        Assert.Empty(snapshot.TodayTasks);
        Assert.Null(snapshot.NetWorthHomeCcy);
    }

    private void SetupMockResponses()
    {
        // Setup score aggregator
        var scoreResult = new LifeOsScoreResult
        {
            LifeScore = 82m,
            HealthIndex = 78m,
            AdherenceIndex = 85m,
            WealthHealthScore = 80m,
            LongevityYearsAdded = 16.5m,
            DimensionScores = new List<DimensionScoreEntry>
            {
                new() { DimensionCode = "health_recovery", Score = 80m, Weight = 0.125m },
                new() { DimensionCode = "relationships", Score = 70m, Weight = 0.125m },
                new() { DimensionCode = "work_contribution", Score = 75m, Weight = 0.125m },
                new() { DimensionCode = "play_adventure", Score = 68m, Weight = 0.125m },
                new() { DimensionCode = "asset_care", Score = 82m, Weight = 0.125m },
                new() { DimensionCode = "create_craft", Score = 71m, Weight = 0.125m },
                new() { DimensionCode = "growth_mind", Score = 88m, Weight = 0.125m },
                new() { DimensionCode = "community_meaning", Score = 73m, Weight = 0.125m }
            },
            CalculatedAt = DateTime.UtcNow
        };

        _mockScoreAggregator
            .Setup(s => s.CalculateAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scoreResult);

        // Setup stats calculator
        var statsResult = new PrimaryStatsResult
        {
            Values = new Dictionary<string, decimal>
            {
                { "strength", 72.5m },
                { "wisdom", 88.0m },
                { "charisma", 81.0m },
                { "composure", 79.0m },
                { "energy", 76.0m },
                { "influence", 74.0m },
                { "vitality", 83.0m }
            },
            DimensionScores = new Dictionary<string, decimal>(),
            CalculatedAt = DateTime.UtcNow
        };

        _mockStatsCalculator
            .Setup(s => s.CalculateAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statsResult);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
