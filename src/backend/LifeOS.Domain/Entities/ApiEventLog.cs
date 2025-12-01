using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class ApiEventLog : BaseEntity
{
    public Guid UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? ApiKeyPrefix { get; set; }
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public string Status { get; set; } = "pending";
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}
