using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.Services;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Daily job (3 AM) to recalculate all dimension scores
/// </summary>
public class ScoreRecomputationJob
{
    private readonly ILifeOSDbContext _context;
    private readonly IScoreCalculator _scoreCalculator;
    private readonly ILogger<ScoreRecomputationJob> _logger;

    public ScoreRecomputationJob(
        ILifeOSDbContext context,
        IScoreCalculator scoreCalculator,
        ILogger<ScoreRecomputationJob> logger)
    {
        _context = context;
        _scoreCalculator = scoreCalculator;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting score recomputation job");

        try
        {
            var users = await _context.Users
                .Where(u => u.Status == Domain.Enums.UserStatus.Active)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            var dimensions = await _context.Dimensions
                .Where(d => d.IsActive)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var scoreCount = 0;

            foreach (var userId in users)
            {
                // Calculate life score
                var lifeScore = await _scoreCalculator.CalculateLifeScoreAsync(userId, cancellationToken);
                
                // Store life score record
                await StoreScoreRecordAsync(userId, "life_score", null, lifeScore, today, cancellationToken);
                scoreCount++;

                // Calculate and store dimension scores
                foreach (var dimension in dimensions)
                {
                    var dimensionScore = await _scoreCalculator.CalculateDimensionScoreAsync(
                        userId, dimension.Id, cancellationToken);
                    
                    await StoreScoreRecordAsync(
                        userId, $"{dimension.Code}_score", dimension.Id, dimensionScore, today, cancellationToken);
                    scoreCount++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Score recomputation completed. Calculated {Count} scores for {UserCount} users", 
                scoreCount, users.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Score recomputation job failed");
            throw;
        }
    }

    private async Task StoreScoreRecordAsync(
        Guid userId, 
        string scoreCode, 
        Guid? dimensionId, 
        decimal value, 
        DateOnly periodStart,
        CancellationToken cancellationToken)
    {
        // Get or create score definition
        var scoreDefinition = await _context.ScoreDefinitions
            .FirstOrDefaultAsync(s => s.Code == scoreCode, cancellationToken);

        if (scoreDefinition == null)
        {
            scoreDefinition = new ScoreDefinition
            {
                Code = scoreCode,
                Name = scoreCode.Replace("_", " ").ToUpperInvariant(),
                DimensionId = dimensionId,
                Formula = "calculated",
                MinScore = 0,
                MaxScore = 100,
                IsActive = true
            };
            _context.ScoreDefinitions.Add(scoreDefinition);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Check for existing record for today
        var existingRecord = await _context.ScoreRecords
            .FirstOrDefaultAsync(r => 
                r.UserId == userId && 
                r.ScoreCode == scoreCode && 
                r.PeriodStart == periodStart, 
                cancellationToken);

        if (existingRecord != null)
        {
            existingRecord.ScoreValue = value;
            existingRecord.CalculatedAt = DateTime.UtcNow;
            existingRecord.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.ScoreRecords.Add(new ScoreRecord
            {
                UserId = userId,
                ScoreCode = scoreCode,
                ScoreValue = value,
                PeriodType = ScorePeriodType.Daily,
                PeriodStart = periodStart,
                PeriodEnd = periodStart,
                CalculatedAt = DateTime.UtcNow
            });
        }
    }
}
