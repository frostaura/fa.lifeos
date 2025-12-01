using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Scores;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LifeOS.Application.Queries.Scores;

public record GetStreaksQuery(
    Guid UserId,
    bool? IsActive = true,
    string? Sort = null
) : IRequest<StreaksResponse>;

public class GetStreaksQueryHandler : IRequestHandler<GetStreaksQuery, StreaksResponse>
{
    private readonly ILifeOSDbContext _context;

    public GetStreaksQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<StreaksResponse> Handle(GetStreaksQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Streaks
            .Include(s => s.Task)
            .Where(s => s.UserId == request.UserId);

        if (request.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == request.IsActive.Value);
        }

        // Only get streaks with actual progress
        query = query.Where(s => s.CurrentStreakLength > 0 || s.LongestStreakLength > 0);

        var sortedQuery = request.Sort switch
        {
            "-currentStreakLength" => query.OrderByDescending(s => s.CurrentStreakLength),
            "currentStreakLength" => query.OrderBy(s => s.CurrentStreakLength),
            "-longestStreakLength" => query.OrderByDescending(s => s.LongestStreakLength),
            "longestStreakLength" => query.OrderBy(s => s.LongestStreakLength),
            "lastSuccessDate" => query.OrderBy(s => s.LastSuccessDate),
            "-lastSuccessDate" => query.OrderByDescending(s => s.LastSuccessDate),
            _ => query.OrderByDescending(s => s.CurrentStreakLength)
        };

        var streaks = await sortedQuery
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new StreaksResponse
        {
            Data = streaks.Select(s => new StreakItemResponse
            {
                Id = s.Id,
                Attributes = new StreakAttributes
                {
                    TaskId = s.TaskId,
                    TaskTitle = s.Task?.Title,
                    MetricCode = s.MetricCode,
                    CurrentStreakLength = s.CurrentStreakLength,
                    LongestStreakLength = s.LongestStreakLength,
                    LastSuccessDate = s.LastSuccessDate,
                    StreakStartDate = s.StreakStartDate,
                    MissCount = s.MissCount,
                    MaxAllowedMisses = s.MaxAllowedMisses,
                    IsActive = s.IsActive
                }
            }).ToList()
        };
    }
}
