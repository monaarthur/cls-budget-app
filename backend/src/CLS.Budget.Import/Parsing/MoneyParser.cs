using System.Globalization;

namespace CLS.Budget.Import.Parsing;

internal static class MoneyParser
{
    public static decimal ParseOrDefault(string? value, decimal defaultValue = 0m)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        var cleaned = value.Trim()
            .Replace("$", "", StringComparison.Ordinal)
            .Replace(",", "", StringComparison.Ordinal);

        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : defaultValue;
    }

    public static decimal? ParseNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : ParseOrDefault(value);
}
