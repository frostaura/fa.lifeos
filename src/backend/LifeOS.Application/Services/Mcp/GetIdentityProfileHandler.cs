using System.Text.Json;
using LifeOS.Application.Commands.Identity;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Interfaces.Mcp;
using LifeOS.Application.Queries.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Services.Mcp;

/// <summary>
/// MCP Tool Handler: lifeos.getIdentityProfile
/// Retrieves user's identity profile with archetype, values, primary stat targets, and linked milestones.
/// </summary>
public class GetIdentityProfileHandler : IMcpToolHandler
{
    private readonly IMediator _mediator;
    
    public string ToolName => "lifeos.getIdentityProfile";
    public string Description => "Get user's identity profile with archetype, values, primary stat targets, and linked milestones";
    
    public GetIdentityProfileHandler(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public async Task<McpToolResponse<object>> HandleAsync(
        string? jsonInput, 
        Guid userId, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Query identity profile via existing handler
            var query = new GetIdentityProfileQuery(userId);
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null || result.Data == null)
            {
                return McpToolResponse<object>.Fail("Identity profile not found. User may need to complete onboarding.");
            }
            
            // Map to MCP format
            var profile = new IdentityProfileMcpDto
            {
                Title = result.Data.Archetype,
                Description = result.Data.ArchetypeDescription,
                Values = result.Data.Values,
                PrimaryStatTargets = result.Data.PrimaryStatTargets,
                LinkedMilestones = result.Data.LinkedMilestones.Select(m => new LinkedMilestoneMcpDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    DimensionCode = "uncategorized" // Will be enhanced in future
                }).ToList()
            };
            
            return McpToolResponse<object>.Ok(profile);
        }
        catch (Exception ex)
        {
            return McpToolResponse<object>.Fail($"Failed to get identity profile: {ex.Message}");
        }
    }
}

/// <summary>
/// MCP Tool Handler: lifeos.updateIdentityTargets
/// Updates primary stat targets in the user's identity profile.
/// </summary>
public class UpdateIdentityTargetsHandler : IMcpToolHandler
{
    private readonly IMediator _mediator;
    
    public string ToolName => "lifeos.updateIdentityTargets";
    public string Description => "Update primary stat targets in identity profile";
    
    public UpdateIdentityTargetsHandler(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public async Task<McpToolResponse<object>> HandleAsync(
        string? jsonInput, 
        Guid userId, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse input
            if (string.IsNullOrEmpty(jsonInput))
            {
                return McpToolResponse<object>.Fail("Input required. Provide 'targets' object with primary stat values.");
            }
            
            var request = JsonSerializer.Deserialize<UpdateIdentityTargetsRequestDto>(
                jsonInput, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (request?.Targets == null || request.Targets.Count == 0)
            {
                return McpToolResponse<object>.Fail("No targets provided. Include 'targets' object with stat names and values (0-100).");
            }
            
            // Validate target values
            var invalidTargets = request.Targets.Where(t => t.Value < 0 || t.Value > 100).ToList();
            if (invalidTargets.Any())
            {
                return McpToolResponse<object>.Fail($"Invalid target values. All values must be 0-100. Invalid: {string.Join(", ", invalidTargets.Select(t => t.Key))}");
            }
            
            // Execute command via existing handler
            var command = new UpdateIdentityTargetsCommand(userId, request.Targets);
            var success = await _mediator.Send(command, cancellationToken);
            
            if (!success)
            {
                return McpToolResponse<object>.Fail("Failed to update identity targets. Identity profile may not exist.");
            }
            
            // Return success response
            var response = new UpdateIdentityTargetsResponseDto
            {
                Updated = true,
                TargetsSet = request.Targets.Count
            };
            
            return McpToolResponse<object>.Ok(response);
        }
        catch (JsonException ex)
        {
            return McpToolResponse<object>.Fail($"Invalid JSON input: {ex.Message}");
        }
        catch (Exception ex)
        {
            return McpToolResponse<object>.Fail($"Failed to update identity targets: {ex.Message}");
        }
    }
}
