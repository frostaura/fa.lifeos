using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

public class FxRate : BaseEntity
{
    public string BaseCurrency { get; set; } = string.Empty;
    public string QuoteCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    
    public DateTime RateTimestamp { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "coingecko";
}
