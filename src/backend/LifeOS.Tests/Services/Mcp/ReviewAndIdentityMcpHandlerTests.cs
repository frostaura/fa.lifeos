using System.Text.Json;
using FluentAssertions;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Services.Mcp;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LifeOS.Tests.Services.Mcp;

/// <summary>
/// Smoke tests for Weekly/Monthly Review and Identity Profile MCP handlers.
/// Full integration tests require more complex setup - these verify basic handler behavior.
/// </summary>
public class ReviewAndIdentityMcpHandlerTests
{
    [Fact]
    public void GetWeeklyReviewHandler_ToolName_IsCorrect()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<LifeOSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new LifeOSDbContext(options);
        var handler = new GetWeeklyReviewHandler(context);

        // Act & Assert
        handler.ToolName.Should().Be("lifeos.getWeeklyReview");
        handler.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetMonthlyReviewHandler_ToolName_IsCorrect()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<LifeOSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new LifeOSDbContext(options);
        var statsCalculator = new Application.Services.PrimaryStatsCalculator(context);
        var handler = new GetMonthlyReviewHandler(context, statsCalculator);

        // Act & Assert
        handler.ToolName.Should().Be("lifeos.getMonthlyReview");
        handler.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetWeeklyReviewHandler_WithEmptyDatabase_ReturnsSuccessWithDefaultData()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<LifeOSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new LifeOSDbContext(options);
        var handler = new GetWeeklyReviewHandler(context);
        var userId = Guid.NewGuid();

        // Act
        var result = await handler.HandleAsync(null, userId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        var review = JsonSerializer.Deserialize<WeeklyReviewDto>(
            JsonSerializer.Serialize(result.Data));
        review.Should().NotBeNull();
        review!.Period.Should().NotBeNull();
        review.FocusActions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMonthlyReviewHandler_WithEmptyDatabase_ReturnsSuccessWithDefaultData()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<LifeOSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new LifeOSDbContext(options);
        var statsCalculator = new Application.Services.PrimaryStatsCalculator(context);
        var handler = new GetMonthlyReviewHandler(context, statsCalculator);
        var userId = Guid.NewGuid();

        // Act
        var result = await handler.HandleAsync(null, userId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        var review = JsonSerializer.Deserialize<MonthlyReviewDto>(
            JsonSerializer.Serialize(result.Data));
        review.Should().NotBeNull();
        review!.Period.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWeeklyReviewHandler_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<LifeOSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new LifeOSDbContext(options);
        var handler = new GetWeeklyReviewHandler(context);
        var userId = Guid.NewGuid();

        // Act
        var result = await handler.HandleAsync("{ invalid }", userId, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("JSON");
    }

    [Fact]
    public async Task GetMonthlyReviewHandler_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<LifeOSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new LifeOSDbContext(options);
        var statsCalculator = new Application.Services.PrimaryStatsCalculator(context);
        var handler = new GetMonthlyReviewHandler(context, statsCalculator);
        var userId = Guid.NewGuid();

        // Act
        var result = await handler.HandleAsync("{ invalid }", userId, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("JSON");
    }
}
