using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Interfaces.Mcp;
using LifeOS.Application.Services;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Services.Mcp;

/// <summary>
/// MCP Tool Handler: lifeos.completeTask
/// Marks a task as completed, creates TaskCompletion record, and updates streak.
/// Optionally records a metric value alongside completion.
/// </summary>
public class CompleteTaskHandler : IMcpToolHandler
{
    private readonly ILifeOSDbContext _context;
    private readonly IStreakService _streakService;
    
    public string ToolName => "lifeos.completeTask";
    public string Description => "Complete a task and update its streak, optionally record metric value";
    
    public CompleteTaskHandler(ILifeOSDbContext context, IStreakService streakService)
    {
        _context = context;
        _streakService = streakService;
    }
    
    public async Task<McpToolResponse<object>> HandleAsync(
        string? jsonInput, 
        Guid userId, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse input
            CompleteTaskRequestDto? request = null;
            if (!string.IsNullOrEmpty(jsonInput))
            {
                request = JsonSerializer.Deserialize<CompleteTaskRequestDto>(
                    jsonInput, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            
            if (request?.TaskId == null || request.TaskId == Guid.Empty)
            {
                return McpToolResponse<object>.Fail("TaskId is required");
            }
            
            var completionTime = request.Timestamp ?? DateTime.UtcNow;
            
            // Load task with streak
            var task = await _context.Tasks
                .Include(t => t.Streaks)
                .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.UserId == userId, cancellationToken);
            
            if (task == null)
            {
                return McpToolResponse<object>.Fail($"Task with ID {request.TaskId} not found");
            }
            
            // Check if already completed today
            var completionDate = DateOnly.FromDateTime(completionTime);
            var existingCompletion = await _context.TaskCompletions
                .Where(tc => tc.TaskId == request.TaskId)
                .Where(tc => DateOnly.FromDateTime(tc.CompletedAt) == completionDate)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (existingCompletion != null)
            {
                return McpToolResponse<object>.Fail($"Task already completed today at {existingCompletion.CompletedAt:yyyy-MM-dd HH:mm}");
            }
            
            // Create TaskCompletion record
            var taskCompletion = new TaskCompletion
            {
                Id = Guid.NewGuid(),
                TaskId = request.TaskId,
                UserId = userId,
                CompletedAt = completionTime,
                CompletionType = CompletionType.Manual,
                ValueNumber = request.ValueNumber,
                Notes = null
            };
            
            _context.TaskCompletions.Add(taskCompletion);
            
            // Update or create streak
            var streak = task.Streaks.FirstOrDefault(s => s.TaskId == task.Id && s.IsActive);
            
            if (streak == null)
            {
                // Create new streak
                streak = new Streak
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TaskId = task.Id,
                    IsActive = true,
                    CurrentStreakLength = 0,
                    LongestStreakLength = 0,
                    ConsecutiveMisses = 0,
                    RiskPenaltyScore = 0,
                    StreakStartDate = completionDate
                };
                
                _context.Streaks.Add(streak);
            }
            
            // Update streak on success
            _streakService.UpdateStreakOnSuccess(streak, completionDate);
            
            // Optionally record metric if provided
            Guid? metricRecordId = null;
            bool metricRecorded = false;
            
            if (request.ValueNumber.HasValue && !string.IsNullOrEmpty(task.LinkedMetricCode))
            {
                // Check if metric definition exists
                var metricDef = await _context.MetricDefinitions
                    .FirstOrDefaultAsync(m => m.Code == task.LinkedMetricCode && m.IsActive, cancellationToken);
                
                if (metricDef != null)
                {
                    var metricRecord = new MetricRecord
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        MetricCode = task.LinkedMetricCode,
                        ValueNumber = request.ValueNumber.Value,
                        RecordedAt = completionTime,
                        Source = "task_completion"
                    };
                    
                    _context.MetricRecords.Add(metricRecord);
                    metricRecordId = metricRecord.Id;
                    metricRecorded = true;
                }
            }
            
            // Save all changes
            await _context.SaveChangesAsync(cancellationToken);
            
            // Build response
            var response = new CompleteTaskResponseDto
            {
                TaskCompletionId = taskCompletion.Id,
                UpdatedStreak = new UpdatedStreakDto
                {
                    CurrentStreakLength = streak.CurrentStreakLength,
                    LongestStreakLength = streak.LongestStreakLength,
                    RiskPenaltyScore = streak.RiskPenaltyScore,
                    ConsecutiveMisses = streak.ConsecutiveMisses
                },
                MetricRecorded = metricRecorded,
                MetricRecordId = metricRecordId
            };
            
            return McpToolResponse<object>.Ok(response);
        }
        catch (JsonException ex)
        {
            return McpToolResponse<object>.Fail($"Invalid JSON input: {ex.Message}");
        }
        catch (Exception ex)
        {
            return McpToolResponse<object>.Fail($"Failed to complete task: {ex.Message}");
        }
    }
}
