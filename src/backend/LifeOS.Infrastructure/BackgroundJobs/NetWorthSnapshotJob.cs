using LifeOS.Application.Common.Interfaces;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LifeOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Daily job (runs at 11:59 PM) to capture net worth snapshots for all active users
/// </summary>
public class NetWorthSnapshotJob
{
    private readonly ILifeOSDbContext _context;
    private readonly ILogger<NetWorthSnapshotJob> _logger;

    public NetWorthSnapshotJob(
        ILifeOSDbContext context,
        ILogger<NetWorthSnapshotJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting net worth snapshot job");

        try
        {
            var users = await _context.Users
                .Where(u => u.Status == UserStatus.Active)
                .Select(u => new { u.Id, u.HomeCurrency })
                .ToListAsync(cancellationToken);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var snapshotCount = 0;

            foreach (var user in users)
            {
                // Check if snapshot already exists for today
                var existingSnapshot = await _context.NetWorthSnapshots
                    .FirstOrDefaultAsync(s => s.UserId == user.Id && s.SnapshotDate == today, 
                        cancellationToken);

                if (existingSnapshot != null)
                {
                    // Update existing snapshot
                    await UpdateSnapshotAsync(existingSnapshot, user.Id, user.HomeCurrency, cancellationToken);
                }
                else
                {
                    // Create new snapshot
                    await CreateSnapshotAsync(user.Id, user.HomeCurrency, today, cancellationToken);
                }
                
                snapshotCount++;
            }

            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Net worth snapshot completed. Captured {Count} snapshots", snapshotCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Net worth snapshot job failed");
            throw;
        }
    }

    private async Task CreateSnapshotAsync(
        Guid userId, 
        string homeCurrency, 
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var (totalAssets, totalLiabilities, breakdownByType, breakdownByCurrency, accountCount) = 
            await CalculateNetWorthAsync(userId, cancellationToken);

        var snapshot = new NetWorthSnapshot
        {
            UserId = userId,
            SnapshotDate = date,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            NetWorth = totalAssets - totalLiabilities,
            HomeCurrency = homeCurrency,
            BreakdownByType = JsonSerializer.Serialize(breakdownByType),
            BreakdownByCurrency = JsonSerializer.Serialize(breakdownByCurrency),
            AccountCount = accountCount
        };

        _context.NetWorthSnapshots.Add(snapshot);
    }

    private async Task UpdateSnapshotAsync(
        NetWorthSnapshot snapshot,
        Guid userId, 
        string homeCurrency,
        CancellationToken cancellationToken)
    {
        var (totalAssets, totalLiabilities, breakdownByType, breakdownByCurrency, accountCount) = 
            await CalculateNetWorthAsync(userId, cancellationToken);

        snapshot.TotalAssets = totalAssets;
        snapshot.TotalLiabilities = totalLiabilities;
        snapshot.NetWorth = totalAssets - totalLiabilities;
        snapshot.HomeCurrency = homeCurrency;
        snapshot.BreakdownByType = JsonSerializer.Serialize(breakdownByType);
        snapshot.BreakdownByCurrency = JsonSerializer.Serialize(breakdownByCurrency);
        snapshot.AccountCount = accountCount;
        snapshot.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<(decimal assets, decimal liabilities, 
        Dictionary<string, decimal> byType, Dictionary<string, decimal> byCurrency, int count)> 
        CalculateNetWorthAsync(Guid userId, CancellationToken cancellationToken)
    {
        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync(cancellationToken);

        var totalAssets = accounts.Where(a => !a.IsLiability).Sum(a => a.CurrentBalance);
        var totalLiabilities = accounts.Where(a => a.IsLiability).Sum(a => a.CurrentBalance);

        // Breakdown by account type
        var byType = accounts
            .GroupBy(a => a.AccountType.ToString())
            .ToDictionary(
                g => g.Key,
                g => g.Sum(a => a.IsLiability ? -a.CurrentBalance : a.CurrentBalance)
            );

        // Breakdown by currency
        var byCurrency = accounts
            .GroupBy(a => a.Currency)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(a => a.IsLiability ? -a.CurrentBalance : a.CurrentBalance)
            );

        return (totalAssets, totalLiabilities, byType, byCurrency, accounts.Count);
    }
}
