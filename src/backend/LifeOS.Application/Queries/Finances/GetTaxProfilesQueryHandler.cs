using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Finances;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LifeOS.Application.Queries.Finances;

public class GetTaxProfilesQueryHandler : IRequestHandler<GetTaxProfilesQuery, TaxProfileListResponse>
{
    private readonly ILifeOSDbContext _context;

    public GetTaxProfilesQueryHandler(ILifeOSDbContext context)
    {
        _context = context;
    }

    public async Task<TaxProfileListResponse> Handle(GetTaxProfilesQuery request, CancellationToken cancellationToken)
    {
        var profiles = await _context.TaxProfiles
            .AsNoTracking()
            .Where(t => t.UserId == request.UserId)
            .OrderByDescending(t => t.TaxYear)
            .ToListAsync(cancellationToken);

        return new TaxProfileListResponse
        {
            Data = profiles.Select(t => new TaxProfileItemResponse
            {
                Id = t.Id,
                Type = "taxProfile",
                Attributes = new TaxProfileAttributes
                {
                    Name = t.Name,
                    TaxYear = t.TaxYear,
                    CountryCode = t.CountryCode,
                    Brackets = ParseBrackets(t.Brackets),
                    UifRate = t.UifRate,
                    UifCap = t.UifCap,
                    VatRate = t.VatRate,
                    IsVatRegistered = t.IsVatRegistered,
                    TaxRebates = ParseRebates(t.TaxRebates),
                    IsActive = t.IsActive
                }
            }).ToList()
        };
    }

    private static List<TaxBracket>? ParseBrackets(string? json)
    {
        if (string.IsNullOrEmpty(json) || json == "[]")
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<TaxBracket>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private static TaxRebates? ParseRebates(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<TaxRebates>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }
}
