using System.ComponentModel;
using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.DTOs.Metrics;
using LifeOS.Application.Interfaces;
using LifeOS.Application.Queries.Metrics;
using LifeOS.Application.Services.Mcp;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

// Use aliases to avoid ambiguity
using McpRecordMetricsRequest = LifeOS.Application.DTOs.Mcp.RecordMetricsRequest;
using McpRecordMetricsResponse = LifeOS.Application.DTOs.Mcp.RecordMetricsResponse;

namespace LifeOS.Api.Mcp.Tools;

/// <summary>
/// MCP Tools for metrics recording and management.
/// </summary>
[McpServerToolType]
public class MetricsTools
{
    private readonly IMetricIngestionService _metricIngestionService;
    private readonly ILifeOSDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly IMcpApiKeyValidator _apiKeyValidator;

    public MetricsTools(
        IMetricIngestionService metricIngestionService,
        ILifeOSDbContext dbContext,
        IMediator mediator,
        IMcpApiKeyValidator apiKeyValidator)
    {
        _metricIngestionService = metricIngestionService;
        _dbContext = dbContext;
        _mediator = mediator;
        _apiKeyValidator = apiKeyValidator;
    }

    /// <summary>
    /// List available metric definitions with their configuration.
    /// </summary>
    [McpServerTool(Name = "listMetrics"), Description("List available metric definitions with codes, units, and value types. Use this to discover what metrics can be recorded. Example response: { Success: true, Data: { Metrics: [ { Code: \"sleep_hours\", Name: \"Sleep Hours\", Unit: \"hours\", ValueType: \"number\" } ], TotalCount: 1, AvailableDimensions: [ \"health\" ] }, Error: null }")]
    public async Task<McpToolResponse<ListMetricsResponse>> ListMetrics(
        [Description("API key for authentication")] string apiKey,
        [Description("Request parameters")] ListMetricsRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<ListMetricsResponse>.Fail(authResult.Error!);

        var query = _dbContext.MetricDefinitions
            .Include(m => m.Dimension)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.DimensionCode))
            query = query.Where(m => m.Dimension != null && m.Dimension.Code == request.DimensionCode);

        if (request.OnlyActive)
            query = query.Where(m => m.IsActive);

        var definitions = await query
            .OrderBy(m => m.Dimension != null ? m.Dimension.Code : "zzz")
            .ThenBy(m => m.Name)
            .ToListAsync(cancellationToken);

        Dictionary<string, (decimal? Value, DateTime? RecordedAt)>? latestValues = null;
        if (request.IncludeLatestValue)
        {
            var codes = definitions.Select(m => m.Code).ToList();
            var latestRecords = await _dbContext.MetricRecords
                .Where(r => r.UserId == authResult.UserId && codes.Contains(r.MetricCode))
                .GroupBy(r => r.MetricCode)
                .Select(g => new { MetricCode = g.Key, Latest = g.OrderByDescending(r => r.RecordedAt).FirstOrDefault() })
                .ToListAsync(cancellationToken);
            latestValues = latestRecords.ToDictionary(r => r.MetricCode, r => (r.Latest?.ValueNumber, r.Latest?.RecordedAt));
        }

        var availableDimensions = await _dbContext.MetricDefinitions
            .Where(m => m.Dimension != null && m.IsActive)
            .Select(m => m.Dimension!.Code)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);

        var response = new ListMetricsResponse
        {
            Metrics = definitions.Select(m =>
            {
                var dto = new MetricDefinitionSummary
                {
                    Id = m.Id,
                    Code = m.Code,
                    Name = m.Name,
                    Description = m.Description ?? string.Empty,
                    DimensionCode = m.Dimension?.Code ?? string.Empty,
                    Unit = m.Unit ?? string.Empty,
                    ValueType = m.ValueType.ToString().ToLower(),
                    AggregationType = m.AggregationType.ToString().ToLower(),
                    EnumValues = m.EnumValues?.ToList() ?? new(),
                    MinValue = m.MinValue,
                    MaxValue = m.MaxValue,
                    TargetValue = m.TargetValue,
                    TargetDirection = m.TargetDirection.ToString(),
                    Weight = m.Weight,
                    IsActive = m.IsActive
                };
                if (latestValues != null && latestValues.TryGetValue(m.Code, out var lv))
                {
                    dto.LatestValue = lv.Value;
                    dto.LatestRecordedAt = lv.RecordedAt;
                }
                return dto;
            }).ToList(),
            TotalCount = definitions.Count,
            AvailableDimensions = availableDimensions
        };

        return McpToolResponse<ListMetricsResponse>.Ok(response);
    }

    /// <summary>
    /// Record multiple metrics in a single call.
    /// </summary>
    [McpServerTool(Name = "recordMetrics"), Description("Record multiple metrics at once. Use listMetrics to discover available metric codes. Example response: { Success: true, Data: { CreatedCount: 2, SkippedMetrics: [ \"unknown_code\" ], Errors: [], Details: {} }, Error: null }")]
    public async Task<McpToolResponse<McpRecordMetricsResponse>> RecordMetrics(
        [Description("API key for authentication")] string apiKey,
        [Description("Request parameters")] McpRecordMetricsRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<McpRecordMetricsResponse>.Fail(authResult.Error!);

        if (request.Entries == null || request.Entries.Count == 0)
            return McpToolResponse<McpRecordMetricsResponse>.Fail("No metric entries provided.");

        var metricsDict = new Dictionary<string, object>();
        foreach (var entry in request.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Code)) continue;
            object? value = entry.ValueNumber.HasValue ? entry.ValueNumber.Value
                : entry.ValueBoolean.HasValue ? entry.ValueBoolean.Value
                : entry.ValueString;
            if (value != null) metricsDict[entry.Code] = value;
        }

        if (metricsDict.Count == 0)
            return McpToolResponse<McpRecordMetricsResponse>.Fail("No valid metric values provided.");

        var ingestionRequest = new MetricRecordRequest
        {
            Timestamp = DateTime.UtcNow,
            Source = request.Source,
            Metrics = metricsDict
        };

        var result = await _metricIngestionService.ProcessNestedMetrics(ingestionRequest, authResult.UserId, cancellationToken);

        return McpToolResponse<McpRecordMetricsResponse>.Ok(new McpRecordMetricsResponse
        {
            CreatedCount = result.CreatedRecords,
            SkippedMetrics = result.IgnoredMetrics,
            Errors = result.Errors,
            Details = new()
        });
    }

    /// <summary>
    /// Get metric history with optional aggregation.
    /// </summary>
    [McpServerTool(Name = "getMetricHistory"), Description("Get historical data for one or more metrics with optional aggregation. Example response: { Success: true, Data: { MetricData: { sleep_hours: { Code: \"sleep_hours\", DataPoints: [ { Timestamp: \"2025-12-12T00:00:00Z\", Value: 7.5, Source: \"mcp\" } ], AverageValue: 7.5 } } }, Error: null }")]
    public async Task<McpToolResponse<GetMetricHistoryResponse>> GetMetricHistory(
        [Description("API key for authentication")] string apiKey,
        [Description("Request parameters")] GetMetricHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<GetMetricHistoryResponse>.Fail(authResult.Error!);

        if (string.IsNullOrEmpty(request.MetricCodes))
            return McpToolResponse<GetMetricHistoryResponse>.Fail("MetricCodes is required.");

        var codes = request.MetricCodes.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var historyResult = await _mediator.Send(new GetMetricHistoryQuery(
            authResult.UserId,
            codes,
            request.FromDate,
            request.ToDate,
            request.Granularity,
            Math.Min(request.Limit, 1000)),
            cancellationToken);

        var response = new GetMetricHistoryResponse
        {
            MetricData = historyResult.Data.ToDictionary(
                kvp => kvp.Key,
                kvp => new MetricHistoryData
                {
                    Code = kvp.Key,
                    DataPoints = kvp.Value.Points.Select(p => new MetricDataPoint
                    {
                        Timestamp = p.Timestamp,
                        Value = p.Value,
                        Source = p.Source ?? string.Empty
                    }).ToList(),
                    TargetValue = kvp.Value.TargetValue,
                    MinValue = kvp.Value.Points.Any() ? kvp.Value.Points.Min(p => p.Value) : null,
                    MaxValue = kvp.Value.Points.Any() ? kvp.Value.Points.Max(p => p.Value) : null,
                    AverageValue = kvp.Value.Points.Any() ? kvp.Value.Points.Average(p => p.Value) : null
                })
        };

        return McpToolResponse<GetMetricHistoryResponse>.Ok(response);
    }
}
