using LifeOS.Application.Common.Interfaces;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Services;

public interface IAchievementService
{
    Task<List<UnlockedAchievement>> EvaluateAchievementsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<AchievementDto>> GetUserAchievementsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserXpDto> GetUserXpAsync(Guid userId, CancellationToken cancellationToken = default);
}

public record UnlockedAchievement(Achievement Achievement, string Context);
public record AchievementDto(string Code, string Name, string Description, string Icon, string Tier, int XpValue, bool Unlocked, DateTime? UnlockedAt);
public record UserXpDto(long TotalXp, int Level, int WeeklyXp, int XpToNextLevel);

public class AchievementService : IAchievementService
{
    private readonly ILifeOSDbContext _context;
    
    // Level thresholds (XP required for each level)
    private static readonly int[] LevelThresholds = { 0, 100, 250, 500, 1000, 2000, 3500, 5000, 7500, 10000, 15000, 25000, 50000, 100000 };
    
    public AchievementService(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<List<UnlockedAchievement>> EvaluateAchievementsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unlockedAchievements = new List<UnlockedAchievement>();
        
        // Get all active achievements not yet unlocked by user
        var unlockedIds = await _context.UserAchievements
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.AchievementId)
            .ToListAsync(cancellationToken);
            
        var lockedAchievements = await _context.Achievements
            .Where(a => a.IsActive && !unlockedIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
        
        // Gather evaluation context
        var evalContext = await BuildEvaluationContextAsync(userId, cancellationToken);
        
        foreach (var achievement in lockedAchievements)
        {
            if (EvaluateCondition(achievement.UnlockCondition, evalContext, out var unlockContext))
            {
                // Unlock the achievement
                var userAchievement = new UserAchievement
                {
                    UserId = userId,
                    AchievementId = achievement.Id,
                    UnlockedAt = DateTime.UtcNow,
                    Progress = 100,
                    UnlockContext = unlockContext
                };
                
                _context.UserAchievements.Add(userAchievement);
                
                // Award XP
                await AwardXpAsync(userId, achievement.XpValue, cancellationToken);
                
                unlockedAchievements.Add(new UnlockedAchievement(achievement, unlockContext));
            }
        }
        
        if (unlockedAchievements.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        
        return unlockedAchievements;
    }

    public async Task<List<AchievementDto>> GetUserAchievementsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var achievements = await _context.Achievements
            .Where(a => a.IsActive)
            .OrderBy(a => a.SortOrder)
            .ToListAsync(cancellationToken);
            
        var userAchievements = await _context.UserAchievements
            .Where(ua => ua.UserId == userId)
            .ToDictionaryAsync(ua => ua.AchievementId, ua => ua, cancellationToken);
        
        return achievements.Select(a => new AchievementDto(
            a.Code,
            a.Name,
            a.Description,
            a.Icon,
            a.Tier,
            a.XpValue,
            userAchievements.ContainsKey(a.Id),
            userAchievements.TryGetValue(a.Id, out var ua) ? ua.UnlockedAt : null
        )).ToList();
    }

    public async Task<UserXpDto> GetUserXpAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userXp = await _context.UserXPs
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        
        if (userXp == null)
        {
            return new UserXpDto(0, 1, 0, LevelThresholds[1]);
        }
        
        var xpToNextLevel = userXp.Level < LevelThresholds.Length 
            ? LevelThresholds[userXp.Level] - (int)userXp.TotalXp 
            : 0;
        
        return new UserXpDto(userXp.TotalXp, userXp.Level, userXp.WeeklyXp, Math.Max(0, xpToNextLevel));
    }

    private async Task AwardXpAsync(Guid userId, int xp, CancellationToken cancellationToken)
    {
        // First check the local change tracker (for unsaved entities), then query the database
        var userXp = _context.UserXPs.Local.FirstOrDefault(x => x.UserId == userId)
            ?? await _context.UserXPs.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        
        var currentWeekStart = DateOnly.FromDateTime(StartOfWeek(DateTime.UtcNow, DayOfWeek.Monday));
        
        if (userXp == null)
        {
            userXp = new UserXP
            {
                UserId = userId,
                TotalXp = 0,
                Level = 1,
                WeeklyXp = 0,
                WeekStartDate = currentWeekStart
            };
            _context.UserXPs.Add(userXp);
        }
        
        // Reset weekly XP if new week
        if (userXp.WeekStartDate < currentWeekStart)
        {
            userXp.WeeklyXp = 0;
            userXp.WeekStartDate = currentWeekStart;
        }
        
        userXp.TotalXp += xp;
        userXp.WeeklyXp += xp;
        
        // Calculate new level
        for (int i = LevelThresholds.Length - 1; i >= 0; i--)
        {
            if (userXp.TotalXp >= LevelThresholds[i])
            {
                userXp.Level = i + 1;
                break;
            }
        }
    }

    private async Task<Dictionary<string, object>> BuildEvaluationContextAsync(Guid userId, CancellationToken cancellationToken)
    {
        var context = new Dictionary<string, object>();
        
        // Net worth
        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync(cancellationToken);
        var netWorth = accounts.Where(a => !a.IsLiability).Sum(a => a.CurrentBalance) -
                       accounts.Where(a => a.IsLiability).Sum(a => a.CurrentBalance);
        context["netWorth"] = netWorth;
        
        // Streaks
        var maxStreak = await _context.Streaks
            .Where(s => s.UserId == userId && s.IsActive)
            .MaxAsync(s => (int?)s.CurrentStreakLength, cancellationToken) ?? 0;
        context["maxStreak"] = maxStreak;
        
        var longestEverStreak = await _context.Streaks
            .Where(s => s.UserId == userId)
            .MaxAsync(s => (int?)s.LongestStreakLength, cancellationToken) ?? 0;
        context["longestStreak"] = longestEverStreak;
        
        // Completed milestones
        var completedMilestones = await _context.Milestones
            .CountAsync(m => m.UserId == userId && m.Status == MilestoneStatus.Completed, cancellationToken);
        context["completedMilestones"] = completedMilestones;
        
        // Account count
        context["accountCount"] = accounts.Count;
        
        // Metric records count
        var metricCount = await _context.MetricRecords
            .CountAsync(m => m.UserId == userId, cancellationToken);
        context["metricRecords"] = metricCount;
        
        // Transaction count
        var transactionCount = await _context.Transactions
            .CountAsync(t => t.UserId == userId, cancellationToken);
        context["transactions"] = transactionCount;
        
        // Days using app (since first record)
        var firstRecord = await _context.MetricRecords
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.RecordedAt)
            .Select(m => m.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var daysActive = firstRecord != default 
            ? (DateTime.UtcNow - firstRecord).Days 
            : 0;
        context["daysActive"] = daysActive;
        
        return context;
    }

    private static bool EvaluateCondition(string condition, Dictionary<string, object> context, out string unlockContext)
    {
        unlockContext = "";
        
        // Simple condition parser
        // Format: "netWorth >= 1000000" or "maxStreak >= 30"
        var parts = condition.Split(' ');
        if (parts.Length < 3) return false;
        
        var key = parts[0];
        var op = parts[1];
        if (!decimal.TryParse(parts[2], out var threshold)) return false;
        
        if (!context.TryGetValue(key, out var valueObj)) return false;
        var value = Convert.ToDecimal(valueObj);
        
        var result = op switch
        {
            ">=" => value >= threshold,
            ">" => value > threshold,
            "<=" => value <= threshold,
            "<" => value < threshold,
            "==" => value == threshold,
            _ => false
        };
        
        if (result)
        {
            unlockContext = $"{key}: {value}";
        }
        
        return result;
    }
    
    private static DateTime StartOfWeek(DateTime dt, DayOfWeek startOfWeek)
    {
        int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
        return dt.AddDays(-1 * diff).Date;
    }
}
