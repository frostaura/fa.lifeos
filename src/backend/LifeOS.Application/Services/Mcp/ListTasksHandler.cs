using System.Text.Json;
using System.Text.RegularExpressions;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Interfaces.Mcp;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Services.Mcp;

/// <summary>
/// MCP Tool Handler: lifeos.listTasks
/// Lists tasks with optional filtering by dimension and completion status.
/// Includes streak information and last completion timestamp.
/// </summary>
public class ListTasksHandler : IMcpToolHandler
{
    private readonly ILifeOSDbContext _context;
    
    public string ToolName => "lifeos.listTasks";
    public string Description => "List tasks with optional dimension filtering and streak information";
    
    public ListTasksHandler(ILifeOSDbContext context)
    {
        _context = context;
    }
    
    public async Task<McpToolResponse<object>> HandleAsync(
        string? jsonInput, 
        Guid userId, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse input
            ListTasksRequestDto? request = null;
            if (!string.IsNullOrEmpty(jsonInput))
            {
                request = JsonSerializer.Deserialize<ListTasksRequestDto>(
                    jsonInput, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            
            request ??= new ListTasksRequestDto();
            
            // Build query
            var query = _context.Tasks
                .Where(t => t.UserId == userId)
                .Include(t => t.Dimension)
                .Include(t => t.Streaks)
                .Include(t => t.TaskCompletions)
                .AsQueryable();
            
            // Apply filters
            if (!string.IsNullOrEmpty(request.DimensionCode))
            {
                query = query.Where(t => t.Dimension != null && t.Dimension.Code == request.DimensionCode);
            }
            
            if (request.OnlyActive)
            {
                query = query.Where(t => t.IsActive);
            }
            
            if (!request.IncludeCompleted)
            {
                query = query.Where(t => !t.IsCompleted);
            }
            
            // Execute query
            var tasks = await query
                .OrderBy(t => t.Dimension != null ? t.Dimension.Code : "zzz")
                .ThenBy(t => t.Title)
                .Take(100) // Limit to 100 tasks for performance
                .ToListAsync(cancellationToken);
            
            // Map to DTOs
            var taskDtos = tasks.Select(t =>
            {
                var streak = t.Streaks.FirstOrDefault(s => s.TaskId == t.Id && s.IsActive);
                var lastCompletion = t.TaskCompletions
                    .OrderByDescending(tc => tc.CompletedAt)
                    .FirstOrDefault();
                
                return new TaskSummaryDto
                {
                    Id = t.Id,
                    DimensionCode = t.Dimension?.Code ?? "uncategorized",
                    Title = t.Title,
                    Emoji = ExtractEmoji(t.Title),
                    Frequency = t.Frequency.ToString().ToLower(),
                    IsHabit = t.TaskType == TaskType.Habit,
                    LastCompletedAt = lastCompletion?.CompletedAt,
                    Streak = streak != null ? new StreakInfoDto
                    {
                        CurrentStreakLength = streak.CurrentStreakLength,
                        LongestStreakLength = streak.LongestStreakLength,
                        RiskPenaltyScore = streak.RiskPenaltyScore,
                        ConsecutiveMisses = streak.ConsecutiveMisses
                    } : null,
                    LinkedMetricCode = t.MetricCode,
                    TargetValue = t.TargetValue,
                    TargetComparison = t.TargetComparison?.ToString().ToLower()
                };
            }).ToList();
            
            var response = new ListTasksResponseDto
            {
                Tasks = taskDtos
            };
            
            return McpToolResponse<object>.Ok(response);
        }
        catch (JsonException ex)
        {
            return McpToolResponse<object>.Fail($"Invalid JSON input: {ex.Message}");
        }
        catch (Exception ex)
        {
            return McpToolResponse<object>.Fail($"Failed to list tasks: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Extracts emoji from the beginning of a string.
    /// Handles compound emoji with Zero Width Joiner (ZWJ) and variation selectors.
    /// </summary>
    private static string? ExtractEmoji(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        
        // Match emoji at start: emoji characters + zero-width joiners + variation selectors + spaces
        var match = Regex.Match(text, @"^[\p{So}\p{Sk}\p{Cs}\u200d\ufe0f\ufe0e]+\s*");
        
        return match.Success && match.Value.Trim().Length > 0 ? match.Value.Trim() : null;
    }
}
