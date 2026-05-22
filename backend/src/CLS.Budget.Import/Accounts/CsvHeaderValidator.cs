using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace CLS.Budget.Import.Accounts;

internal sealed class CsvValidationReport
{
    public required string FilePath { get; init; }
    public IReadOnlyList<string> FileHeaders { get; init; } = [];
    public IReadOnlyList<string> NormalizedHeaders { get; init; } = [];
    public IReadOnlyList<ExpectedColumn> ExpectedColumns { get; init; } = [];
    public IReadOnlyList<string> Issues { get; init; } = [];
    public int DataRowCount { get; init; }
    public bool IsValid => Issues.Count == 0;
}

internal sealed record ExpectedColumn(string PropertyName, params string[] AcceptedHeaders);

internal static class CsvHeaderValidator
{
    private static readonly ExpectedColumn[] Expected =
    [
        new("Active", "Active"),
        new("Category", "Category"),
        new("AccountBill", "Account Bill", "Account / Bill"),
        new("AccountNumber", "Account Number", "Account #"),
        new("DueDate", "Due Date"),
        new("DueDay", "Due Day"),
        new("CurrentBalanceOwe", "Current Balance Owe", "Current Balance / Owe"),
        new("MonthlyPayment", "Monthly Payment"),
        new("PaidWith", "Paid With"),
        new("MonthlyDueOverride", "Monthly Due Override"),
        new("PaymentDateExtra", "Payment Date Extra", "Payment Date / Extra"),
        new("Status", "Status"),
        new("Notes", "Notes"),
        new("Interest", "Interest"),
        new("LastUpdate", "LastUpdate", "Last Update"),
        new("AvailableCredit", "Available Credit"),
        new("CreditLimit", "Credit Limit"),
        new("Url", "Url", "URL")
    ];

    public static CsvValidationReport Validate(string filePath)
    {
        var issues = new List<string>();

        if (!File.Exists(filePath))
        {
            return new CsvValidationReport
            {
                FilePath = filePath,
                Issues = [$"File not found: {filePath}"]
            };
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args => NormalizeHeaderForMatch(args.Header)
        };

        string[] fileHeaders;

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, config))
        {
            csv.Read();
            csv.ReadHeader();
            fileHeaders = csv.HeaderRecord ?? [];
        }

        var dataRows = MonthlyBillsCsvReader.Read(filePath).Count;

        var normalized = fileHeaders.Select(NormalizeHeaderForMatch).ToList();
        var normalizedSet = new HashSet<string>(normalized, StringComparer.OrdinalIgnoreCase);

        foreach (var column in Expected)
        {
            var accepted = column.AcceptedHeaders
                .Select(NormalizeHeaderForMatch)
                .ToList();

            if (!accepted.Any(normalizedSet.Contains))
            {
                issues.Add($"Missing column for {column.PropertyName}. Expected header: {column.AcceptedHeaders[0]}");
            }
        }

        for (var i = 0; i < fileHeaders.Length; i++)
        {
            var raw = fileHeaders[i];
            var norm = normalized[i];

            if (raw.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(
                    $"Column {i + 1} header is a URL, not \"Url\". Excel likely used the first row's link as the header. Rename it to \"Url\" in Excel and re-export.");
            }
            else if (!Expected.Any(e => e.AcceptedHeaders.Select(NormalizeHeaderForMatch).Contains(norm)))
            {
                issues.Add($"Unexpected column {i + 1}: \"{raw}\"");
            }
        }

        issues.AddRange(DetectShiftedMoneyColumns(filePath));

        return new CsvValidationReport
        {
            FilePath = filePath,
            FileHeaders = fileHeaders,
            NormalizedHeaders = normalized,
            ExpectedColumns = Expected,
            Issues = issues,
            DataRowCount = dataRows
        };
    }

    internal static string NormalizeHeaderForMatch(string header)
    {
        var trimmed = header.Trim();
        if (trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return "Url";
        }

        var normalized = trimmed
            .Replace("/", " ", StringComparison.Ordinal)
            .Replace("#", "Number", StringComparison.Ordinal);

        while (normalized.Contains("  ", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
        }

        return normalized;
    }

    private static IEnumerable<string> DetectShiftedMoneyColumns(string filePath)
    {
        var rows = MonthlyBillsCsvReader.Read(filePath);
        var warnings = new List<string>();

        foreach (var row in rows)
        {
            var paymentInWrongColumn =
                string.IsNullOrWhiteSpace(row.MonthlyPayment)
                && LooksLikeMoney(row.PaymentDateExtra);

            if (paymentInWrongColumn)
            {
                warnings.Add(
                    $"Possible column shift: \"{row.AccountBill}\" has money in Payment Date / Extra ({row.PaymentDateExtra}) but Monthly Payment is empty. Check Excel columns align with the header row.");
            }
        }

        if (warnings.Count > 0)
        {
            yield return warnings[0];
            if (warnings.Count > 1)
            {
                yield return $"...and {warnings.Count - 1} more row(s) with the same pattern.";
            }
        }
    }

    private static bool LooksLikeMoney(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && (value.Contains('$', StringComparison.Ordinal) || decimal.TryParse(
            value.Replace("$", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal),
            out _));
}
