using System.ComponentModel;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Interfaces;
using LifeOS.Application.Services.Mcp;
using ModelContextProtocol.Server;

namespace LifeOS.Api.Mcp.Tools;

/// <summary>
/// MCP Tools for dashboard and overview operations.
/// </summary>
[McpServerToolType]
public class DashboardTools
{
    private readonly ILifeOSScoreAggregator _scoreAggregator;
    private readonly IPrimaryStatsCalculator _statsCalculator;
    private readonly ILifeOSDbContext _dbContext;
    private readonly IMcpApiKeyValidator _apiKeyValidator;

    public DashboardTools(
        ILifeOSScoreAggregator scoreAggregator,
        IPrimaryStatsCalculator statsCalculator,
        ILifeOSDbContext dbContext,
        IMcpApiKeyValidator apiKeyValidator)
    {
        _scoreAggregator = scoreAggregator;
        _statsCalculator = statsCalculator;
        _dbContext = dbContext;
        _apiKeyValidator = apiKeyValidator;
    }

    /// <summary>
    /// Get comprehensive dashboard snapshot with scores, stats, tasks, and events.
    /// </summary>
    [McpServerTool(Name = "getDashboardSnapshot"), Description("Get comprehensive dashboard snapshot with LifeOS scores, primary stats, today's tasks, net worth, and upcoming events. Example response: { Success: true, Data: { LifeScore: 82.3, PrimaryStats: { strength: 65 }, TodayTasks: [ { TaskId: <guid>, Title: \"Meditate\", IsCompleted: false } ], NetWorthHomeCcy: 12345.67 }, Error: null }")]
    public async Task<McpToolResponse<DashboardSnapshotDto>> GetDashboardSnapshot(
        [Description("API key for authentication")] string apiKey,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<DashboardSnapshotDto>.Fail(authResult.Error!);

        var handler = new GetDashboardSnapshotHandler(_scoreAggregator, _statsCalculator, _dbContext);
        var result = await handler.HandleAsync(null, authResult.UserId, cancellationToken);

        if (!result.Success)
            return McpToolResponse<DashboardSnapshotDto>.Fail(result.Error ?? "Failed to get dashboard snapshot.");

        return McpToolResponse<DashboardSnapshotDto>.Ok((DashboardSnapshotDto)result.Data!);
    }
}
