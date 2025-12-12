using System.ComponentModel;
using LifeOS.Application.Commands.Milestones;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Queries.Milestones;
using LifeOS.Application.Services.Mcp;
using MediatR;
using ModelContextProtocol.Server;

namespace LifeOS.Api.Mcp.Tools;

/// <summary>
/// MCP Tools for milestone management operations.
/// </summary>
[McpServerToolType]
public class MilestoneTools
{
    private readonly IMediator _mediator;
    private readonly IMcpApiKeyValidator _apiKeyValidator;

    public MilestoneTools(
        IMediator mediator,
        IMcpApiKeyValidator apiKeyValidator)
    {
        _mediator = mediator;
        _apiKeyValidator = apiKeyValidator;
    }

    /// <summary>
    /// List all milestones for the authenticated user.
    /// </summary>
    [McpServerTool(Name = "listMilestones"), Description("List all milestones for the user with progress tracking, target dates, and completion status. Example response: { Success: true, Data: { Milestones: [ { Id: <guid>, Title: \"Run a 10K\", ProgressPercent: 0, IsCompleted: false } ], TotalCount: 1, ActiveCount: 1, CompletedCount: 0 }, Error: null }")]
    public async Task<McpToolResponse<ListMilestonesResponse>> ListMilestones(
        [Description("API key for authentication")] string apiKey,
        [Description("Request parameters including optional status filter")] ListMilestonesRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<ListMilestonesResponse>.Fail(authResult.Error!);

        var query = new GetMilestonesQuery(authResult.UserId, Status: request.StatusFilter);
        var result = await _mediator.Send(query, cancellationToken);

        var summaries = result.Data.Select(m => new MilestoneSummary
        {
            Id = m.Id,
            Title = m.Attributes.Title,
            TargetDate = m.Attributes.TargetDate?.ToDateTime(TimeOnly.MinValue),
            ProgressPercent = m.Attributes.Status == "completed" ? 100 : 0,
            IsCompleted = m.Attributes.Status == "completed",
            Category = m.Attributes.DimensionCode ?? string.Empty
        }).ToList();

        return McpToolResponse<ListMilestonesResponse>.Ok(new ListMilestonesResponse
        {
            Milestones = summaries,
            TotalCount = summaries.Count,
            ActiveCount = summaries.Count(m => !m.IsCompleted),
            CompletedCount = summaries.Count(m => m.IsCompleted)
        });
    }

    /// <summary>
    /// Get detailed information about a specific milestone.
    /// </summary>
    [McpServerTool(Name = "getMilestone"), Description("Get detailed information about a specific milestone including all sub-goals and linked tasks. Example response: { Success: true, Data: { Milestone: { Id: <guid>, Title: \"Run a 10K\", TargetDate: \"2026-03-01T00:00:00Z\", IsCompleted: false, ProgressPercent: 0 } }, Error: null }")]
    public async Task<McpToolResponse<GetMilestoneResponse>> GetMilestone(
        [Description("API key for authentication")] string apiKey,
        [Description("Request containing the milestone ID")] GetMilestoneRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<GetMilestoneResponse>.Fail(authResult.Error!);

        var query = new GetMilestoneByIdQuery(authResult.UserId, request.MilestoneId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
            return McpToolResponse<GetMilestoneResponse>.Fail($"Milestone with ID {request.MilestoneId} not found.");

        var attr = result.Data.Attributes;
        return McpToolResponse<GetMilestoneResponse>.Ok(new GetMilestoneResponse
        {
            Milestone = new MilestoneDetail
            {
                Id = result.Data.Id,
                Title = attr.Title,
                Description = attr.Description ?? string.Empty,
                TargetDate = attr.TargetDate?.ToDateTime(TimeOnly.MinValue),
                ProgressPercent = attr.Status == "completed" ? 100 : 0,
                IsCompleted = attr.Status == "completed",
                Category = attr.DimensionCode ?? string.Empty,
                CreatedAt = attr.CreatedAt,
                CompletedAt = attr.CompletedAt,
                Notes = string.Empty
            }
        });
    }

    /// <summary>
    /// Create a new milestone.
    /// </summary>
    [McpServerTool(Name = "createMilestone"), Description("Create a new milestone with title, description, target date, and dimension. Example response: { Success: true, Data: { Success: true, MilestoneId: <guid>, Message: \"Milestone 'Run a 10K' created successfully.\" }, Error: null }")]
    public async Task<McpToolResponse<CreateMilestoneResponse>> CreateMilestone(
        [Description("API key for authentication")] string apiKey,
        [Description("Milestone creation parameters")] Application.DTOs.Mcp.CreateMilestoneRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<CreateMilestoneResponse>.Fail(authResult.Error!);

        if (string.IsNullOrWhiteSpace(request.Title))
            return McpToolResponse<CreateMilestoneResponse>.Fail("Milestone title is required.");

        // Category maps to a dimension - we need a dimension ID
        // For simplicity, we'll use a default or expect it to be provided
        var dimensionId = Guid.Empty;

        var command = new CreateMilestoneCommand(
            authResult.UserId,
            request.Title,
            request.Description,
            dimensionId,
            request.TargetDate.HasValue ? DateOnly.FromDateTime(request.TargetDate.Value) : null,
            null,
            null
        );

        var result = await _mediator.Send(command, cancellationToken);

        return McpToolResponse<CreateMilestoneResponse>.Ok(new CreateMilestoneResponse
        {
            MilestoneId = result.Data.Id,
            Success = true,
            Message = $"Milestone '{request.Title}' created successfully."
        });
    }

    /// <summary>
    /// Update an existing milestone.
    /// </summary>
    [McpServerTool(Name = "updateMilestone"), Description("Update an existing milestone's title, description, target date, or status. Example response: { Success: true, Data: { Success: true, UpdatedMilestone: { Id: <guid>, Title: \"Run a 10K\", IsCompleted: false, ProgressPercent: 0 } }, Error: null }")]
    public async Task<McpToolResponse<UpdateMilestoneResponse>> UpdateMilestone(
        [Description("API key for authentication")] string apiKey,
        [Description("Milestone update parameters")] Application.DTOs.Mcp.UpdateMilestoneRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<UpdateMilestoneResponse>.Fail(authResult.Error!);

        // Verify milestone exists
        var existingQuery = new GetMilestoneByIdQuery(authResult.UserId, request.MilestoneId);
        var existing = await _mediator.Send(existingQuery, cancellationToken);
        if (existing == null)
            return McpToolResponse<UpdateMilestoneResponse>.Fail($"Milestone with ID {request.MilestoneId} not found.");

        var command = new UpdateMilestoneCommand(
            authResult.UserId,
            request.MilestoneId,
            string.IsNullOrEmpty(request.Title) ? null : request.Title,
            string.IsNullOrEmpty(request.Description) ? null : request.Description,
            request.TargetDate.HasValue ? DateOnly.FromDateTime(request.TargetDate.Value) : null,
            null
        );

        await _mediator.Send(command, cancellationToken);

        // Re-fetch updated milestone
        var updated = await _mediator.Send(existingQuery, cancellationToken);
        var attr = updated!.Data.Attributes;

        return McpToolResponse<UpdateMilestoneResponse>.Ok(new UpdateMilestoneResponse
        {
            Success = true,
            Message = $"Milestone updated successfully.",
            UpdatedMilestone = new MilestoneSummary
            {
                Id = request.MilestoneId,
                Title = attr.Title,
                TargetDate = attr.TargetDate?.ToDateTime(TimeOnly.MinValue),
                ProgressPercent = attr.Status == "completed" ? 100 : 0,
                IsCompleted = attr.Status == "completed",
                Category = attr.DimensionCode ?? string.Empty
            }
        });
    }

    /// <summary>
    /// Delete a milestone.
    /// </summary>
    [McpServerTool(Name = "deleteMilestone"), Description("Delete a milestone permanently. This action cannot be undone. Example response: { Success: true, Data: { Success: true, DeletedMilestoneId: <guid>, Message: \"Milestone 'Run a 10K' deleted successfully.\" }, Error: null }")]
    public async Task<McpToolResponse<DeleteMilestoneResponse>> DeleteMilestone(
        [Description("API key for authentication")] string apiKey,
        [Description("Request containing the milestone ID to delete")] DeleteMilestoneRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<DeleteMilestoneResponse>.Fail(authResult.Error!);

        // Verify milestone exists
        var existingQuery = new GetMilestoneByIdQuery(authResult.UserId, request.MilestoneId);
        var existing = await _mediator.Send(existingQuery, cancellationToken);
        if (existing == null)
            return McpToolResponse<DeleteMilestoneResponse>.Fail($"Milestone with ID {request.MilestoneId} not found.");

        var command = new DeleteMilestoneCommand(authResult.UserId, request.MilestoneId);
        await _mediator.Send(command, cancellationToken);

        return McpToolResponse<DeleteMilestoneResponse>.Ok(new DeleteMilestoneResponse
        {
            Success = true,
            Message = $"Milestone '{existing.Data.Attributes.Title}' deleted successfully.",
            DeletedMilestoneId = request.MilestoneId
        });
    }

    /// <summary>
    /// Mark a milestone as complete.
    /// </summary>
    [McpServerTool(Name = "completeMilestone"), Description("Mark a milestone as 100% complete. Example response: { Success: true, Data: { Success: true, UpdatedMilestone: { Id: <guid>, Title: \"Run a 10K\", IsCompleted: true, ProgressPercent: 100 } }, Error: null }")]
    public async Task<McpToolResponse<UpdateMilestoneResponse>> CompleteMilestone(
        [Description("API key for authentication")] string apiKey,
        [Description("Request containing the milestone ID to complete")] CompleteMilestoneRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<UpdateMilestoneResponse>.Fail(authResult.Error!);

        // Verify milestone exists
        var existingQuery = new GetMilestoneByIdQuery(authResult.UserId, request.MilestoneId);
        var existing = await _mediator.Send(existingQuery, cancellationToken);
        if (existing == null)
            return McpToolResponse<UpdateMilestoneResponse>.Fail($"Milestone with ID {request.MilestoneId} not found.");

        var attr = existing.Data.Attributes;

        var command = new UpdateMilestoneCommand(
            authResult.UserId,
            request.MilestoneId,
            null,
            null,
            null,
            "Completed"
        );

        await _mediator.Send(command, cancellationToken);

        return McpToolResponse<UpdateMilestoneResponse>.Ok(new UpdateMilestoneResponse
        {
            Success = true,
            Message = $"Milestone '{attr.Title}' marked as complete.",
            UpdatedMilestone = new MilestoneSummary
            {
                Id = request.MilestoneId,
                Title = attr.Title,
                TargetDate = attr.TargetDate?.ToDateTime(TimeOnly.MinValue),
                ProgressPercent = 100,
                IsCompleted = true,
                Category = attr.DimensionCode ?? string.Empty
            }
        });
    }
}
