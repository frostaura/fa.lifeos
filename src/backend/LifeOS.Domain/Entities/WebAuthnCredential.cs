using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class WebAuthnCredential : BaseEntity
{
    public Guid UserId { get; set; }
    public byte[] CredentialId { get; set; } = Array.Empty<byte>();
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();
    public byte[] UserHandle { get; set; } = Array.Empty<byte>();
    public uint SignatureCounter { get; set; }
    public string CredType { get; set; } = "public-key";
    public Guid AaGuid { get; set; }
    public string? DeviceName { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    
    // Navigation
    public virtual User User { get; set; } = null!;
}
