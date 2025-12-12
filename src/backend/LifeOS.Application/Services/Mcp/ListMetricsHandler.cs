using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Interfaces.Mcp;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Services.Mcp;

/// <summary>
/// MCP Tool Handler: lifeos.listMetrics
/// Lists available metric definitions with their configuration and latest values.
/// Provides AI assistants with a catalog of metrics they can record.
/// </summary>
public class ListMetricsHandler : IMcpToolHandler
{
    private readonly ILifeOSDbContext _context;

    public string ToolName => "lifeos.listMetrics";
    public string Description => "List available metric definitions with codes, types, and configuration";

    public ListMetricsHandler(ILifeOSDbContext context)
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
            ListMetricsRequestDto? request = null;
            if (!string.IsNullOrEmpty(jsonInput))
            {
                request = JsonSerializer.Deserialize<ListMetricsRequestDto>(
                    jsonInput,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            request ??= new ListMetricsRequestDto();

            // Build query for metric definitions
            var query = _context.MetricDefinitions
                .Include(m => m.Dimension)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.DimensionCode))
            {
                query = query.Where(m => m.Dimension != null && m.Dimension.Code == request.DimensionCode);
            }

            if (request.OnlyActive)
            {
                query = query.Where(m => m.IsActive);
            }

            if (request.Tags != null && request.Tags.Length > 0)
            {
                // Filter by tags - metric must have at least one matching tag
                query = query.Where(m => m.Tags != null && m.Tags.Any(t => request.Tags.Contains(t)));
            }

            // Execute query
            var metricDefinitions = await query
                .OrderBy(m => m.Dimension != null ? m.Dimension.Code : "zzz")
                .ThenBy(m => m.Name)
                .ToListAsync(cancellationToken);

            // Get latest values if requested
            Dictionary<string, (decimal? Value, DateTime? RecordedAt)>? latestValues = null;
            if (request.IncludeLatestValue)
            {
                var metricCodes = metricDefinitions.Select(m => m.Code).ToList();

                // Get the most recent record for each metric code for this user
                var latestRecords = await _context.MetricRecords
                    .Where(r => r.UserId == userId && metricCodes.Contains(r.MetricCode))
                    .GroupBy(r => r.MetricCode)
                    .Select(g => new
                    {
                        MetricCode = g.Key,
                        LatestRecord = g.OrderByDescending(r => r.RecordedAt).FirstOrDefault()
                    })
                    .ToListAsync(cancellationToken);

                latestValues = latestRecords.ToDictionary(
                    r => r.MetricCode,
                    r => (r.LatestRecord?.ValueNumber, r.LatestRecord?.RecordedAt));
            }

            // Get unique dimension codes for filtering help
            var availableDimensions = await _context.MetricDefinitions
                .Where(m => m.Dimension != null && m.IsActive)
                .Select(m => m.Dimension!.Code)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(cancellationToken);

            // Map to DTOs
            var metricDtos = metricDefinitions.Select(m =>
            {
                var dto = new MetricDefinitionSummaryDto
                {
                    Id = m.Id,
                    Code = m.Code,
                    Name = m.Name,
                    Description = m.Description,
                    DimensionCode = m.Dimension?.Code,
                    Unit = m.Unit,
                    ValueType = m.ValueType.ToString().ToLower(),
                    AggregationType = m.AggregationType.ToString().ToLower(),
                    EnumValues = m.EnumValues,
                    MinValue = m.MinValue,
                    MaxValue = m.MaxValue,
                    TargetValue = m.TargetValue,
                    TargetDirection = FormatTargetDirection(m.TargetDirection.ToString()),
                    Weight = m.Weight,
                    Icon = m.Icon,
                    Tags = m.Tags,
                    IsDerived = m.IsDerived,
                    IsActive = m.IsActive
                };

                // Add latest value if available
                if (latestValues != null && latestValues.TryGetValue(m.Code, out var latestValue))
                {
                    dto.LatestValue = latestValue.Value;
                    dto.LatestRecordedAt = latestValue.RecordedAt;
                }

                return dto;
            }).ToList();

            var response = new ListMetricsResponseDto
            {
                Metrics = metricDtos,
                TotalCount = metricDtos.Count,
                AvailableDimensions = availableDimensions
            };

            return McpToolResponse<object>.Ok(response);
        }
        catch (JsonException ex)
        {
            return McpToolResponse<object>.Fail($"Invalid JSON input: {ex.Message}");
        }
        catch (Exception ex)
        {
            return McpToolResponse<object>.Fail($"Failed to list metrics: {ex.Message}");
        }
    }

    /// <summary>
    /// Formats target direction enum to camelCase string.
    /// </summary>
    private static string FormatTargetDirection(string direction)
    {
        return direction switch
        {
            "AtOrAbove" => "atOrAbove",
            "AtOrBelow" => "atOrBelow",
            "Range" => "range",
            "Exact" => "exact",
            _ => direction.ToLower()
        };
    }
}
