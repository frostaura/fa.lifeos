namespace LifeOS.Application.DTOs.Mcp;

/// <summary>
/// Response DTO for identity profile MCP tool.
/// </summary>
public class IdentityProfileMcpDto
{
    /// <summary>Identity archetype title (e.g., "God of Mind-Power")</summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>Detailed description of the archetype</summary>
    public string? Description { get; set; }
    
    /// <summary>Core values list</summary>
    public List<string> Values { get; set; } = new();
    
    /// <summary>Primary stat targets (0-100 for each stat)</summary>
    public Dictionary<string, int> PrimaryStatTargets { get; set; } = new();
    
    /// <summary>Linked milestones supporting this identity</summary>
    public List<LinkedMilestoneMcpDto> LinkedMilestones { get; set; } = new();
}

/// <summary>
/// Linked milestone entry for identity profile.
/// </summary>
public class LinkedMilestoneMcpDto
{
    /// <summary>Milestone ID</summary>
    public Guid Id { get; set; }
    
    /// <summary>Milestone title</summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>Dimension code this milestone belongs to</summary>
    public string DimensionCode { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for updating identity targets MCP tool.
/// </summary>
public class UpdateIdentityTargetsRequestDto
{
    /// <summary>
    /// Primary stat targets to update. Keys are stat names (lowercase), values are 0-100.
    /// Example: { "wisdom": 95, "energy": 85 }
    /// </summary>
    public Dictionary<string, int> Targets { get; set; } = new();
}

/// <summary>
/// Response DTO for update identity targets MCP tool.
/// </summary>
public class UpdateIdentityTargetsResponseDto
{
    /// <summary>Whether targets were successfully updated</summary>
    public bool Updated { get; set; }
    
    /// <summary>Number of targets that were set</summary>
    public int TargetsSet { get; set; }
}
