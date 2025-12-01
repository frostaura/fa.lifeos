using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Scores;
using LifeOS.Application.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Scores;

public record GetScoresQuery(Guid UserId) : IRequest<ScoresResponse>;

public class GetScoresQueryHandler : IRequestHandler<GetScoresQuery, ScoresResponse>
{
    private readonly ILifeOSDbContext _context;
    private readonly IScoreCalculator _scoreCalculator;

    public GetScoresQueryHandler(ILifeOSDbContext context, IScoreCalculator scoreCalculator)
    {
        _context = context;
        _scoreCalculator = scoreCalculator;
    }

    public async Task<ScoresResponse> Handle(GetScoresQuery request, CancellationToken cancellationToken)
    {
        // Get all dimensions
        var dimensions = await _context.Dimensions
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Calculate dimension scores
        var scores = new List<ScoreItemResponse>();
        decimal weightedTotal = 0;
        decimal totalWeight = 0;

        foreach (var dimension in dimensions)
        {
            var dimensionScore = await _scoreCalculator.CalculateDimensionScoreAsync(
                request.UserId, 
                dimension.Id, 
                cancellationToken);

            // Get previous score for comparison
            var previousScore = await _context.ScoreRecords
                .Where(s => s.UserId == request.UserId 
                    && s.ScoreCode == $"{dimension.Code}_score"
                    && s.PeriodStart < DateOnly.FromDateTime(DateTime.UtcNow))
                .OrderByDescending(s => s.PeriodStart)
                .Select(s => (decimal?)s.ScoreValue)
                .FirstOrDefaultAsync(cancellationToken);

            var change = previousScore.HasValue ? dimensionScore - previousScore.Value : (decimal?)null;
            var changePercent = previousScore.HasValue && previousScore.Value > 0 
                ? (change / previousScore.Value) * 100 
                : null;

            scores.Add(new ScoreItemResponse
            {
                Id = dimension.Id,
                Attributes = new ScoreAttributes
                {
                    Code = $"{dimension.Code}_score",
                    Name = $"{dimension.Name} Score",
                    Description = dimension.Description,
                    DimensionId = dimension.Id,
                    DimensionCode = dimension.Code,
                    CurrentValue = dimensionScore,
                    PreviousValue = previousScore,
                    Change = change,
                    ChangePercent = changePercent,
                    PeriodType = "daily",
                    MinScore = 0,
                    MaxScore = 100
                }
            });

            weightedTotal += dimensionScore * dimension.DefaultWeight;
            totalWeight += dimension.DefaultWeight;
        }

        // Calculate life score (weighted average)
        var lifeScore = totalWeight > 0 ? weightedTotal / totalWeight : 0;

        // Get previous life score
        var previousLifeScore = await _context.ScoreRecords
            .Where(s => s.UserId == request.UserId 
                && s.ScoreCode == "life_score"
                && s.PeriodStart < DateOnly.FromDateTime(DateTime.UtcNow))
            .OrderByDescending(s => s.PeriodStart)
            .Select(s => (decimal?)s.ScoreValue)
            .FirstOrDefaultAsync(cancellationToken);

        var lifeScoreChange = previousLifeScore.HasValue ? lifeScore - previousLifeScore.Value : (decimal?)null;
        var lifeScoreChangePercent = previousLifeScore.HasValue && previousLifeScore.Value > 0 
            ? (lifeScoreChange / previousLifeScore.Value) * 100 
            : null;

        // Insert life score at the beginning
        scores.Insert(0, new ScoreItemResponse
        {
            Id = Guid.Empty,
            Attributes = new ScoreAttributes
            {
                Code = "life_score",
                Name = "Life Score",
                Description = "Weighted aggregate across all dimensions",
                CurrentValue = Math.Round(lifeScore, 1),
                PreviousValue = previousLifeScore,
                Change = lifeScoreChange.HasValue ? Math.Round(lifeScoreChange.Value, 1) : null,
                ChangePercent = lifeScoreChangePercent.HasValue ? Math.Round(lifeScoreChangePercent.Value, 1) : null,
                PeriodType = "daily",
                MinScore = 0,
                MaxScore = 100
            }
        });

        return new ScoresResponse { Data = scores };
    }
}
