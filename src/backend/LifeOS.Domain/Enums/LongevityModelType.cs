namespace LifeOS.Domain.Enums;

public enum LongevityModelType
{
    Threshold = 0,  // Binary: above/below threshold
    Range = 1,      // Linear interpolation within range
    Linear = 2,     // Linear relationship
    Boolean = 3     // True/False state
}
