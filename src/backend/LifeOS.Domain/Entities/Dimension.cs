using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class Dimension : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public decimal DefaultWeight { get; set; } = 0.125m;
    public short SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public virtual ICollection<LifeTask> Tasks { get; set; } = new List<LifeTask>();
    public virtual ICollection<MetricDefinition> MetricDefinitions { get; set; } = new List<MetricDefinition>();
    public virtual ICollection<ScoreDefinition> ScoreDefinitions { get; set; } = new List<ScoreDefinition>();
}
