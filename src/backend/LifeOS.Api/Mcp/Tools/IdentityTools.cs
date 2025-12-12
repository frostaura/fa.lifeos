using System.ComponentModel;
using System.Text.Json;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Services.Mcp;
using MediatR;
using ModelContextProtocol.Server;

namespace LifeOS.Api.Mcp.Tools;

#region Identity Request/Response DTOs

/// <summary>
/// Request to get identity profile.
/// </summary>
public class GetIdentityProfileRequest
{
    // No parameters required - uses authenticated user's profile
}

/// <summary>
/// Response containing the user's identity profile.
/// </summary>
public class IdentityProfileResponse
{
    [Description("The user's archetype or persona")]
    public string Archetype { get; set; } = string.Empty;

    [Description("List of the user's core values")]
    public List<string> CoreValues { get; set; } = new();

    [Description("Current primary stat levels (0-100)")]
    public PrimaryStatLevels CurrentStats { get; set; } = new();

    [Description("Target primary stat levels (0-100)")]
    public PrimaryStatLevels TargetStats { get; set; } = new();

    [Description("Milestones linked to identity development")]
    public List<IdentityMilestoneSummary> LinkedMilestones { get; set; } = new();
}

/// <summary>
/// Primary stat levels for user identity.
/// </summary>
public class PrimaryStatLevels
{
    [Description("Physical strength and fitness level (0-100)")]
    public int Strength { get; set; }

    [Description("Knowledge and learning level (0-100)")]
    public int Wisdom { get; set; }

    [Description("Social skills and communication level (0-100)")]
    public int Charisma { get; set; }

    [Description("Emotional regulation and stress management (0-100)")]
    public int Composure { get; set; }

    [Description("Physical and mental energy level (0-100)")]
    public int Energy { get; set; }

    [Description("Leadership and impact level (0-100)")]
    public int Influence { get; set; }

    [Description("Overall health and wellness level (0-100)")]
    public int Vitality { get; set; }
}

/// <summary>
/// Summary of a milestone linked to identity.
/// </summary>
public class IdentityMilestoneSummary
{
    [Description("Unique identifier of the milestone")]
    public Guid Id { get; set; }

    [Description("Title of the milestone")]
    public string Title { get; set; } = string.Empty;

    [Description("Progress percentage (0-100)")]
    public decimal ProgressPercent { get; set; }

    [Description("Which primary stat this milestone affects")]
    public string LinkedStat { get; set; } = string.Empty;
}

/// <summary>
/// Request to update identity stat targets.
/// </summary>
public class UpdateIdentityTargetsRequest
{
    [Description("Target strength level (0-100)")]
    public int Strength { get; set; }

    [Description("Target wisdom level (0-100)")]
    public int Wisdom { get; set; }

    [Description("Target charisma level (0-100)")]
    public int Charisma { get; set; }

    [Description("Target composure level (0-100)")]
    public int Composure { get; set; }

    [Description("Target energy level (0-100)")]
    public int Energy { get; set; }

    [Description("Target influence level (0-100)")]
    public int Influence { get; set; }

    [Description("Target vitality level (0-100)")]
    public int Vitality { get; set; }
}

/// <summary>
/// Response after updating identity targets.
/// </summary>
public class UpdateIdentityTargetsResponse
{
    [Description("Whether the update was successful")]
    public bool Success { get; set; }

    [Description("Message describing the result")]
    public string Message { get; set; } = string.Empty;

    [Description("The updated target stat levels")]
    public PrimaryStatLevels UpdatedTargets { get; set; } = new();
}

#endregion

/// <summary>
/// MCP Tools for identity profile management.
/// </summary>
[McpServerToolType]
public class IdentityTools
{
    private readonly IMediator _mediator;
    private readonly IMcpApiKeyValidator _apiKeyValidator;

    public IdentityTools(
        IMediator mediator,
        IMcpApiKeyValidator apiKeyValidator)
    {
        _mediator = mediator;
        _apiKeyValidator = apiKeyValidator;
    }

    /// <summary>
    /// Get user's identity profile with archetype, values, primary stat targets, and linked milestones.
    /// </summary>
    [McpServerTool(Name = "getIdentityProfile"), Description("Get user's identity profile including archetype, core values, current and target primary stats, and linked milestones. Example response: { Success: true, Data: { Archetype: \"The Strategist\", CoreValues: [ \"Health\", \"Family\" ], CurrentStats: { Strength: 62, Wisdom: 70 }, TargetStats: { Strength: 75, Wisdom: 80 }, LinkedMilestones: [ { Id: <guid>, Title: \"10K Run\", ProgressPercent: 40, LinkedStat: \"Vitality\" } ] }, Error: null }")]
    public async Task<McpToolResponse<IdentityProfileResponse>> GetIdentityProfile(
        [Description("API key for authentication")] string apiKey,
        [Description("Request parameters (empty object)")] GetIdentityProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<IdentityProfileResponse>.Fail(authResult.Error!);

        var handler = new GetIdentityProfileHandler(_mediator);
        var result = await handler.HandleAsync(null, authResult.UserId, cancellationToken);

        if (!result.Success)
            return McpToolResponse<IdentityProfileResponse>.Fail(result.Error ?? "Failed to get identity profile.");

        // Convert the handler response to our typed response
        var data = result.Data;
        if (data == null)
            return McpToolResponse<IdentityProfileResponse>.Fail("No identity profile data returned.");

        // The handler returns a dynamic object, we need to map it
        var json = JsonSerializer.Serialize(data);
        var response = JsonSerializer.Deserialize<IdentityProfileResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return McpToolResponse<IdentityProfileResponse>.Ok(response ?? new IdentityProfileResponse());
    }

    /// <summary>
    /// Update primary stat targets in the user's identity profile.
    /// </summary>
    [McpServerTool(Name = "updateIdentityTargets"), Description("Update primary stat targets (strength, wisdom, charisma, composure, energy, influence, vitality) in identity profile. All values should be 0-100. Example response: { Success: true, Data: { Success: true, Message: \"Identity targets updated successfully.\", UpdatedTargets: { Strength: 75, Wisdom: 80, Charisma: 60, Composure: 70, Energy: 65, Influence: 55, Vitality: 72 } }, Error: null }")]
    public async Task<McpToolResponse<UpdateIdentityTargetsResponse>> UpdateIdentityTargets(
        [Description("API key for authentication")] string apiKey,
        [Description("Target stat values to update")] UpdateIdentityTargetsRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<UpdateIdentityTargetsResponse>.Fail(authResult.Error!);

        // Validate all values are within range
        var stats = new[] { request.Strength, request.Wisdom, request.Charisma, request.Composure, request.Energy, request.Influence, request.Vitality };
        if (stats.Any(s => s < 0 || s > 100))
            return McpToolResponse<UpdateIdentityTargetsResponse>.Fail("All stat values must be between 0 and 100.");

        var targets = new Dictionary<string, int>
        {
            { "strength", request.Strength },
            { "wisdom", request.Wisdom },
            { "charisma", request.Charisma },
            { "composure", request.Composure },
            { "energy", request.Energy },
            { "influence", request.Influence },
            { "vitality", request.Vitality }
        };

        var input = new { targets };
        var handler = new UpdateIdentityTargetsHandler(_mediator);
        var result = await handler.HandleAsync(JsonSerializer.Serialize(input), authResult.UserId, cancellationToken);

        if (!result.Success)
            return McpToolResponse<UpdateIdentityTargetsResponse>.Fail(result.Error ?? "Failed to update identity targets.");

        return McpToolResponse<UpdateIdentityTargetsResponse>.Ok(new UpdateIdentityTargetsResponse
        {
            Success = true,
            Message = "Identity targets updated successfully.",
            UpdatedTargets = new PrimaryStatLevels
            {
                Strength = request.Strength,
                Wisdom = request.Wisdom,
                Charisma = request.Charisma,
                Composure = request.Composure,
                Energy = request.Energy,
                Influence = request.Influence,
                Vitality = request.Vitality
            }
        });
    }
}
