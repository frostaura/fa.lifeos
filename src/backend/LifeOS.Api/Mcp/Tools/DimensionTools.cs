using System.ComponentModel;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Services.Mcp;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace LifeOS.Api.Mcp.Tools;

/// <summary>
/// MCP Tools for dimension management operations.
/// </summary>
[McpServerToolType]
public class DimensionTools
{
    private readonly IMediator _mediator;
    private readonly ILifeOSDbContext _dbContext;
    private readonly IMcpApiKeyValidator _apiKeyValidator;

    public DimensionTools(
        IMediator mediator,
        ILifeOSDbContext dbContext,
        IMcpApiKeyValidator apiKeyValidator)
    {
        _mediator = mediator;
        _dbContext = dbContext;
        _apiKeyValidator = apiKeyValidator;
    }

    /// <summary>
    /// List all dimensions (life areas) for the authenticated user.
    /// </summary>
    [McpServerTool(Name = "listDimensions"), Description("List all life dimensions (areas like Health, Career, Finance, etc.) with their current scores and weights. Example response: { Success: true, Data: { Dimensions: [ { Id: <guid>, Name: \"Health\", Weight: 20, CurrentScore: 78.2 } ], TotalCount: 1, AverageScore: 78.2 }, Error: null }")]
    public async Task<McpToolResponse<ListDimensionsResponse>> ListDimensions(
        [Description("API key for authentication")] string apiKey,
        [Description("Request parameters")] ListDimensionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<ListDimensionsResponse>.Fail(authResult.Error!);

        var dimensions = await _dbContext.Dimensions
            .Where(d => d.UserId == authResult.UserId)
            .OrderBy(d => d.Name)
            .Select(d => new DimensionSummary
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description ?? string.Empty,
                Weight = d.Weight,
                CurrentScore = d.CurrentScore,
                Color = d.Color ?? "#6366f1"
            })
            .ToListAsync(cancellationToken);

        return McpToolResponse<ListDimensionsResponse>.Ok(new ListDimensionsResponse
        {
            Dimensions = dimensions,
            TotalCount = dimensions.Count,
            AverageScore = dimensions.Any() ? dimensions.Average(d => d.CurrentScore) : 0
        });
    }

    /// <summary>
    /// Get detailed information about a specific dimension.
    /// </summary>
    [McpServerTool(Name = "getDimension"), Description("Get detailed information about a specific life dimension including linked metrics and current performance. Example response: { Success: true, Data: { Dimension: { Id: <guid>, Name: \"Health\", CurrentScore: 78.2, Weight: 20, LinkedMetricCount: 5 } }, Error: null }")]
    public async Task<McpToolResponse<GetDimensionResponse>> GetDimension(
        [Description("API key for authentication")] string apiKey,
        [Description("Request containing the dimension ID")] GetDimensionRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<GetDimensionResponse>.Fail(authResult.Error!);

        var dimension = await _dbContext.Dimensions
            .Where(d => d.Id == request.DimensionId && d.UserId == authResult.UserId)
            .Select(d => new DimensionDetail
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description ?? string.Empty,
                Weight = d.Weight,
                CurrentScore = d.CurrentScore,
                Color = d.Color ?? "#6366f1",
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                LinkedMetricCount = d.MetricDefinitions.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dimension == null)
            return McpToolResponse<GetDimensionResponse>.Fail($"Dimension with ID {request.DimensionId} not found.");

        return McpToolResponse<GetDimensionResponse>.Ok(new GetDimensionResponse
        {
            Dimension = dimension
        });
    }

    /// <summary>
    /// Update the weight of a dimension in the user's life balance calculation.
    /// </summary>
    [McpServerTool(Name = "updateDimensionWeight"), Description("Update the importance weight of a life dimension. Weights affect how the dimension contributes to the overall LifeOS score. Example response: { Success: true, Data: { Success: true, DimensionId: <guid>, DimensionName: \"Health\", PreviousWeight: 15, NewWeight: 20 }, Error: null }")]
    public async Task<McpToolResponse<UpdateDimensionWeightResponse>> UpdateDimensionWeight(
        [Description("API key for authentication")] string apiKey,
        [Description("Request containing dimension ID and new weight")] UpdateDimensionWeightRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<UpdateDimensionWeightResponse>.Fail(authResult.Error!);

        if (request.NewWeight < 0 || request.NewWeight > 100)
            return McpToolResponse<UpdateDimensionWeightResponse>.Fail("Weight must be between 0 and 100.");

        var dimension = await _dbContext.Dimensions
            .FirstOrDefaultAsync(d => d.Id == request.DimensionId && d.UserId == authResult.UserId, cancellationToken);

        if (dimension == null)
            return McpToolResponse<UpdateDimensionWeightResponse>.Fail($"Dimension with ID {request.DimensionId} not found.");

        var previousWeight = dimension.Weight;
        dimension.Weight = request.NewWeight;
        dimension.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return McpToolResponse<UpdateDimensionWeightResponse>.Ok(new UpdateDimensionWeightResponse
        {
            Success = true,
            Message = $"Dimension '{dimension.Name}' weight updated from {previousWeight} to {request.NewWeight}.",
            DimensionId = dimension.Id,
            DimensionName = dimension.Name,
            PreviousWeight = previousWeight,
            NewWeight = request.NewWeight
        });
    }

    /// <summary>
    /// Create a new dimension.
    /// </summary>
    [McpServerTool(Name = "createDimension"), Description("Create a new life dimension to track a specific area of life. Example response: { Success: true, Data: { Success: true, DimensionId: <guid>, Message: \"Dimension 'Health' created successfully.\" }, Error: null }")]
    public async Task<McpToolResponse<CreateDimensionResponse>> CreateDimension(
        [Description("API key for authentication")] string apiKey,
        [Description("Dimension creation parameters")] CreateDimensionRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<CreateDimensionResponse>.Fail(authResult.Error!);

        if (string.IsNullOrWhiteSpace(request.Name))
            return McpToolResponse<CreateDimensionResponse>.Fail("Dimension name is required.");

        // Check for duplicate name
        var existingDimension = await _dbContext.Dimensions
            .AnyAsync(d => d.UserId == authResult.UserId && d.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (existingDimension)
            return McpToolResponse<CreateDimensionResponse>.Fail($"A dimension named '{request.Name}' already exists.");

        var dimension = new LifeOS.Domain.Entities.Dimension
        {
            Id = Guid.NewGuid(),
            UserId = authResult.UserId,
            Name = request.Name,
            Description = request.Description,
            Weight = request.Weight,
            CurrentScore = 0,
            Color = request.Color ?? "#6366f1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Dimensions.Add(dimension);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return McpToolResponse<CreateDimensionResponse>.Ok(new CreateDimensionResponse
        {
            DimensionId = dimension.Id,
            Success = true,
            Message = $"Dimension '{request.Name}' created successfully."
        });
    }

    /// <summary>
    /// Delete a dimension.
    /// </summary>
    [McpServerTool(Name = "deleteDimension"), Description("Delete a life dimension. This will also unlink any associated metrics. Example response: { Success: true, Data: { Success: true, DeletedDimensionId: <guid>, Message: \"Dimension 'Health' deleted successfully.\" }, Error: null }")]
    public async Task<McpToolResponse<DeleteDimensionResponse>> DeleteDimension(
        [Description("API key for authentication")] string apiKey,
        [Description("Request containing the dimension ID to delete")] DeleteDimensionRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<DeleteDimensionResponse>.Fail(authResult.Error!);

        var dimension = await _dbContext.Dimensions
            .Include(d => d.MetricDefinitions)
            .FirstOrDefaultAsync(d => d.Id == request.DimensionId && d.UserId == authResult.UserId, cancellationToken);

        if (dimension == null)
            return McpToolResponse<DeleteDimensionResponse>.Fail($"Dimension with ID {request.DimensionId} not found.");

        // Unlink metrics before deleting
        foreach (var metric in dimension.MetricDefinitions)
        {
            metric.DimensionId = null;
        }

        _dbContext.Dimensions.Remove(dimension);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return McpToolResponse<DeleteDimensionResponse>.Ok(new DeleteDimensionResponse
        {
            Success = true,
            Message = $"Dimension '{dimension.Name}' deleted successfully.",
            DeletedDimensionId = request.DimensionId
        });
    }
}
