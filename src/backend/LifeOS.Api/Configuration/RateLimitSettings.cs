namespace LifeOS.Api.Configuration;

public class RateLimitSettings
{
    public const string SectionName = "RateLimiting";
    
    public int DefaultPermitLimit { get; set; } = 100;
    public int DefaultWindowMinutes { get; set; } = 1;
    
    public int AuthPermitLimit { get; set; } = 5;
    public int AuthWindowMinutes { get; set; } = 15;
    
    public int MetricsPermitLimit { get; set; } = 1000;
    public int MetricsWindowMinutes { get; set; } = 1;
}
