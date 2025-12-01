using LifeOS.Domain.Common;
using LifeOS.Domain.Enums;

namespace LifeOS.Domain.Entities;

public class ScoreRecord : BaseEntity
{
    public Guid UserId { get; set; }
    public string ScoreCode { get; set; } = string.Empty;
    
    public decimal ScoreValue { get; set; }
    
    public ScorePeriodType PeriodType { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    
    public string? Breakdown { get; set; }
    
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ScoreDefinition ScoreDefinition { get; set; } = null!;
}
