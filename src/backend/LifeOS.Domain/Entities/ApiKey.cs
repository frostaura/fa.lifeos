using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class ApiKey : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;  // e.g., "lifeos_abc12345"
    public string KeyHash { get; set; } = string.Empty;    // SHA256 hash of full key
    public string Scopes { get; set; } = "metrics:write";  // Comma-separated scopes
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsRevoked { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
}
