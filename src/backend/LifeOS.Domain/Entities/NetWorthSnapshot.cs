using LifeOS.Domain.Common;

namespace LifeOS.Domain.Entities;

/// <summary>
/// Captures actual daily net worth snapshots (distinct from simulation projections)
/// </summary>
public class NetWorthSnapshot : BaseEntity
{
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Date of the snapshot (YYYY-MM-DD)
    /// </summary>
    public DateOnly SnapshotDate { get; set; }
    
    /// <summary>
    /// Total assets in home currency at time of snapshot
    /// </summary>
    public decimal TotalAssets { get; set; }
    
    /// <summary>
    /// Total liabilities in home currency at time of snapshot
    /// </summary>
    public decimal TotalLiabilities { get; set; }
    
    /// <summary>
    /// Net worth (TotalAssets - TotalLiabilities) in home currency
    /// </summary>
    public decimal NetWorth { get; set; }
    
    /// <summary>
    /// Home currency at time of snapshot
    /// </summary>
    public string HomeCurrency { get; set; } = "ZAR";
    
    /// <summary>
    /// JSON breakdown by account type
    /// </summary>
    public string BreakdownByType { get; set; } = "{}";
    
    /// <summary>
    /// JSON breakdown by currency
    /// </summary>
    public string BreakdownByCurrency { get; set; } = "{}";
    
    /// <summary>
    /// Number of accounts included in snapshot
    /// </summary>
    public int AccountCount { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}
