using System.Text.Json;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.DTOs.Metrics;
using LifeOS.Application.Interfaces;
using LifeOS.Application.Interfaces.Mcp;

namespace LifeOS.Application.Services.Mcp;

/// <summary>
/// MCP Tool Handler: lifeos.recordMetrics
/// Records multiple metrics in a single call by proxying to existing MetricIngestionService.
/// Supports nested dimension-grouped structure for ergonomic AI usage.
/// </summary>
public class RecordMetricsHandler : IMcpToolHandler
{
    private readonly IMetricIngestionService _metricIngestionService;
    
    public string ToolName => "lifeos.recordMetrics";
    public string Description => "Record multiple metrics in a single call with optional dimension grouping";
    
    public RecordMetricsHandler(IMetricIngestionService metricIngestionService)
    {
        _metricIngestionService = metricIngestionService;
    }
    
    public async Task<McpToolResponse<object>> HandleAsync(
        string? jsonInput, 
        Guid userId, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse input
            RecordMetricsRequestDto? request = null;
            if (!string.IsNullOrEmpty(jsonInput))
            {
                request = JsonSerializer.Deserialize<RecordMetricsRequestDto>(
                    jsonInput, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            
            request ??= new RecordMetricsRequestDto();
            
            // Validate
            if (request.Metrics == null || request.Metrics.Count == 0)
            {
                return McpToolResponse<object>.Fail("No metrics provided. Include 'metrics' object with metric codes and values.");
            }
            
            // Proxy to existing metric ingestion service
            var ingestionRequest = new MetricRecordRequest
            {
                Timestamp = request.Timestamp ?? DateTime.UtcNow,
                Source = request.Source,
                Metrics = request.Metrics
            };
            
            var result = await _metricIngestionService.ProcessNestedMetrics(
                ingestionRequest, 
                userId, 
                cancellationToken);
            
            // Map result to MCP response format
            var responseDto = new RecordMetricsResponseDto
            {
                CreatedRecords = result.CreatedRecords,
                IgnoredMetrics = result.IgnoredMetrics,
                Errors = result.Errors
            };
            
            // If there were any errors, include them in the response but still succeed
            // (partial success is acceptable for batch operations)
            return McpToolResponse<object>.Ok(responseDto);
        }
        catch (JsonException ex)
        {
            return McpToolResponse<object>.Fail($"Invalid JSON input: {ex.Message}");
        }
        catch (Exception ex)
        {
            return McpToolResponse<object>.Fail($"Failed to record metrics: {ex.Message}");
        }
    }
}
