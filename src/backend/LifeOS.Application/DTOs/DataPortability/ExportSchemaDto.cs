namespace LifeOS.Application.DTOs.DataPortability;

public record ExportSchemaDto
{
    public string Version { get; init; } = "1.0.0";
    public string Generator { get; init; } = "LifeOS";
    public DateTime ExportedAt { get; init; } = DateTime.UtcNow;
}
