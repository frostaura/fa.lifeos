namespace LifeOS.Application.DTOs.Identity;

public record IdentityProfileDto
{
    public string Archetype { get; init; } = string.Empty;
    public string? ArchetypeDescription { get; init; }
    public List<string> Values { get; init; } = new();
    public Dictionary<string, int> PrimaryStatTargets { get; init; } = new();
    public List<Guid> LinkedMilestoneIds { get; init; } = new();
    public DateTime? UpdatedAt { get; init; }
}

public record IdentityProfileResponse
{
    public IdentityProfileDataWrapper Data { get; init; } = new();
}

public record IdentityProfileDataWrapper
{
    public string Archetype { get; init; } = string.Empty;
    public string? ArchetypeDescription { get; init; }
    public List<string> Values { get; init; } = new();
    public Dictionary<string, int> PrimaryStatTargets { get; init; } = new();
    public List<LinkedMilestoneDto> LinkedMilestones { get; init; } = new();
}

public record LinkedMilestoneDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
}

public record UpdateIdentityProfileRequest
{
    public string Archetype { get; init; } = string.Empty;
    public string? ArchetypeDescription { get; init; }
    public List<string> Values { get; init; } = new();
    public Dictionary<string, int> PrimaryStatTargets { get; init; } = new();
    public List<Guid> LinkedMilestoneIds { get; init; } = new();
}

public record UpdateIdentityTargetsRequest
{
    public Dictionary<string, int> Targets { get; init; } = new();
}
