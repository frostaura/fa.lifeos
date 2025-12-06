namespace LifeOS.Api.Configuration;

public class Fido2Settings
{
    public const string SectionName = "Fido2";
    
    /// <summary>
    /// The domain that WebAuthn credentials are scoped to (e.g., "localhost" or "lifeos.frostaura.net")
    /// This is the RP ID in WebAuthn terminology
    /// </summary>
    public string ServerDomain { get; set; } = "localhost";
    
    /// <summary>
    /// The human-readable name of the relying party
    /// </summary>
    public string ServerName { get; set; } = "LifeOS";
    
    /// <summary>
    /// List of allowed origins that can make WebAuthn requests
    /// Must include the protocol (http:// or https://)
    /// </summary>
    public string[] Origins { get; set; } = new[] 
    { 
        "http://localhost:5173", 
        "http://localhost:5001", 
        "http://localhost:5000" 
    };
    
    /// <summary>
    /// Timestamp drift tolerance in milliseconds (default: 5 minutes)
    /// </summary>
    public int TimestampDriftTolerance { get; set; } = 300000;
}
