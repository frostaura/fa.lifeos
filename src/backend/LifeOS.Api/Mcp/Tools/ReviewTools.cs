using System.ComponentModel;
using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Interfaces;
using LifeOS.Application.Services.Mcp;
using ModelContextProtocol.Server;

namespace LifeOS.Api.Mcp.Tools;

#region Review Request/Response DTOs

/// <summary>
/// Request to get weekly review data.
/// </summary>
public class GetWeeklyReviewRequest
{
    [Description("Week number (1-52) to get review for, defaults to current week")]
    public int WeekNumber { get; set; }

    [Description("Year to get review for, defaults to current year")]
    public int Year { get; set; }
}

/// <summary>
/// Request to get monthly review data.
/// </summary>
public class GetMonthlyReviewRequest
{
    [Description("Month number (1-12) to get review for, defaults to current month")]
    public int Month { get; set; }

    [Description("Year to get review for, defaults to current year")]
    public int Year { get; set; }
}

#endregion

/// <summary>
/// MCP Tools for weekly and monthly review operations.
/// </summary>
[McpServerToolType]
public class ReviewTools
{
    private readonly ILifeOSDbContext _dbContext;
    private readonly IPrimaryStatsCalculator _statsCalculator;
    private readonly IMcpApiKeyValidator _apiKeyValidator;

    public ReviewTools(
        ILifeOSDbContext dbContext,
        IPrimaryStatsCalculator statsCalculator,
        IMcpApiKeyValidator apiKeyValidator)
    {
        _dbContext = dbContext;
        _statsCalculator = statsCalculator;
        _apiKeyValidator = apiKeyValidator;
    }

    /// <summary>
    /// Get weekly performance review with score changes and streak analysis.
    /// </summary>
    [McpServerTool(Name = "getWeeklyReview"), Description("Get weekly performance review with score changes, streak analysis, and recommended focus actions. Example response: { Success: true, Data: { Period: { Start: \"2025-12-08\", End: \"2025-12-14\" }, HealthIndexChange: { From: 74.0, To: 76.5 }, TopStreaks: [ { TaskTitle: \"Meditate\", StreakLength: 14, DimensionCode: \"mind\" } ], FocusActions: [ \"Keep sleep consistent\" ] }, Error: null }")]
    public async Task<McpToolResponse<WeeklyReviewDto>> GetWeeklyReview(
        [Description("API key for authentication")] string apiKey,
        [Description("Request parameters")] GetWeeklyReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<WeeklyReviewDto>.Fail(authResult.Error!);

        // Calculate week start date from week number and year
        var year = request.Year > 0 ? request.Year : DateTime.UtcNow.Year;
        var weekNum = request.WeekNumber > 0 ? request.WeekNumber : GetIso8601WeekOfYear(DateTime.UtcNow);
        var weekStartDate = FirstDateOfWeekISO8601(year, weekNum);

        var input = new { weekStartDate };
        var handler = new GetWeeklyReviewHandler(_dbContext);
        var result = await handler.HandleAsync(JsonSerializer.Serialize(input), authResult.UserId, cancellationToken);

        if (!result.Success)
            return McpToolResponse<WeeklyReviewDto>.Fail(result.Error ?? "Failed to get weekly review.");

        return McpToolResponse<WeeklyReviewDto>.Ok((WeeklyReviewDto)result.Data!);
    }

    /// <summary>
    /// Get monthly performance review with score trends and milestone progress.
    /// </summary>
    [McpServerTool(Name = "getMonthlyReview"), Description("Get monthly performance review with score trends, net worth changes, identity radar comparison, and milestone progress. Example response: { Success: true, Data: { Period: { Start: \"2025-12-01\", End: \"2025-12-31\" }, LifeScoreChange: { From: 80.1, To: 82.3 }, NetWorthChange: { From: 12000, To: 12345.67 }, MilestoneProgress: [ { MilestoneTitle: \"10K Run\", PercentComplete: 40 } ], TopWins: [ \"Completed weekly review\" ] }, Error: null }")]
    public async Task<McpToolResponse<MonthlyReviewDto>> GetMonthlyReview(
        [Description("API key for authentication")] string apiKey,
        [Description("Request parameters")] GetMonthlyReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<MonthlyReviewDto>.Fail(authResult.Error!);

        var year = request.Year > 0 ? request.Year : DateTime.UtcNow.Year;
        var month = request.Month > 0 && request.Month <= 12 ? request.Month : DateTime.UtcNow.Month;
        var monthStr = $"{year}-{month:D2}";

        var input = new { month = monthStr, year };
        var handler = new GetMonthlyReviewHandler(_dbContext, _statsCalculator);
        var result = await handler.HandleAsync(JsonSerializer.Serialize(input), authResult.UserId, cancellationToken);

        if (!result.Success)
            return McpToolResponse<MonthlyReviewDto>.Fail(result.Error ?? "Failed to get monthly review.");

        return McpToolResponse<MonthlyReviewDto>.Ok((MonthlyReviewDto)result.Data!);
    }

    private static int GetIso8601WeekOfYear(DateTime time)
    {
        var day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            time = time.AddDays(3);
        return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            time, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    private static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
    {
        var jan1 = new DateTime(year, 1, 1);
        var daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
        var firstThursday = jan1.AddDays(daysOffset);
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        var firstWeek = cal.GetWeekOfYear(firstThursday, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        var weekNum = weekOfYear;
        if (firstWeek == 1) weekNum -= 1;
        var result = firstThursday.AddDays(weekNum * 7);
        return result.AddDays(-3);
    }
}
