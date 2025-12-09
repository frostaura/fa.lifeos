namespace LifeOS.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; internal set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
