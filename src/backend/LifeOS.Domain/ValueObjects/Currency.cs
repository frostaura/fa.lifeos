using System.Text.RegularExpressions;

namespace LifeOS.Domain.ValueObjects;

public partial class Currency : IEquatable<Currency>
{
    public string Code { get; }

    public static readonly Currency ZAR = new("ZAR");
    public static readonly Currency USD = new("USD");
    public static readonly Currency EUR = new("EUR");
    public static readonly Currency GBP = new("GBP");
    public static readonly Currency BTC = new("BTC");
    public static readonly Currency ETH = new("ETH");

    private Currency(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentNullException(nameof(code));
            
        if (!CurrencyCodeRegex().IsMatch(code))
            throw new ArgumentException("Currency code must be 3 uppercase letters", nameof(code));

        Code = code.ToUpperInvariant();
    }

    public static Currency FromCode(string code) => new(code);

    public override string ToString() => Code;

    public override bool Equals(object? obj) => obj is Currency other && Equals(other);

    public bool Equals(Currency? other) => other is not null && Code == other.Code;

    public override int GetHashCode() => Code.GetHashCode();

    public static bool operator ==(Currency? left, Currency? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Currency? left, Currency? right) => !(left == right);

    public static implicit operator string(Currency currency) => currency.Code;

    public static explicit operator Currency(string code) => FromCode(code);

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyCodeRegex();
}
