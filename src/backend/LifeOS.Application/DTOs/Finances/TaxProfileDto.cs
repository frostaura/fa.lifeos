namespace LifeOS.Application.DTOs.Finances;

public record TaxProfileDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "Default";
    public int TaxYear { get; init; }
    public string CountryCode { get; init; } = "ZA";
    public List<TaxBracket>? Brackets { get; init; }
    public decimal? UifRate { get; init; }
    public decimal? UifCap { get; init; }
    public decimal? VatRate { get; init; }
    public bool IsVatRegistered { get; init; }
    public TaxRebates? TaxRebates { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record TaxBracket
{
    public decimal Min { get; init; }
    public decimal? Max { get; init; }
    public decimal Rate { get; init; }
    public decimal BaseTax { get; init; }
}

public record TaxRebates
{
    public decimal? Primary { get; init; }
    public decimal? Secondary { get; init; }
    public decimal? Tertiary { get; init; }
}

public record TaxProfileListResponse
{
    public List<TaxProfileItemResponse> Data { get; init; } = new();
}

public record TaxProfileItemResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "taxProfile";
    public TaxProfileAttributes Attributes { get; init; } = new();
}

public record TaxProfileAttributes
{
    public string Name { get; init; } = "Default";
    public int TaxYear { get; init; }
    public string CountryCode { get; init; } = "ZA";
    public List<TaxBracket>? Brackets { get; init; }
    public decimal? UifRate { get; init; }
    public decimal? UifCap { get; init; }
    public decimal? VatRate { get; init; }
    public bool IsVatRegistered { get; init; }
    public TaxRebates? TaxRebates { get; init; }
    public bool IsActive { get; init; }
}

public record TaxProfileDetailResponse
{
    public TaxProfileItemResponse Data { get; init; } = new();
}

public record CreateTaxProfileRequest
{
    public string Name { get; init; } = "Default";
    public int TaxYear { get; init; }
    public string CountryCode { get; init; } = "ZA";
    public List<TaxBracket>? Brackets { get; init; }
    public decimal? UifRate { get; init; }
    public decimal? UifCap { get; init; }
    public decimal? VatRate { get; init; }
    public bool IsVatRegistered { get; init; }
    public TaxRebates? TaxRebates { get; init; }
}

public record UpdateTaxProfileRequest
{
    public string? Name { get; init; }
    public List<TaxBracket>? Brackets { get; init; }
    public decimal? UifRate { get; init; }
    public decimal? UifCap { get; init; }
    public decimal? VatRate { get; init; }
    public bool? IsVatRegistered { get; init; }
    public TaxRebates? TaxRebates { get; init; }
    public bool? IsActive { get; init; }
}
