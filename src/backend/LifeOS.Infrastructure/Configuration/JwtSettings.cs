namespace LifeOS.Infrastructure.Configuration;

public class JwtSettings
{
    public const string SectionName = "Jwt";
    
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "lifeos-api";
    public string Audience { get; set; } = "lifeos-client";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
