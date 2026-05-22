using System.Globalization;
using System.Text;
using CLS.Budget.Application.Accounts.Dtos;
using CLS.Budget.Import.Parsing;

namespace CLS.Budget.Import.Accounts;

internal static class MonthlyBillsRowMapper
{
    private const string ImportEmail = "import@cls-budget.local";
    private const string ImportPhone = "000-000-0000";
    private const string DefaultUrl = "https://import.cls-budget.local";

    public static CreateAccountRequest ToCreateRequest(MonthlyBillsCsvRow row)
    {
        var name = row.AccountBill!.Trim();
        var number = string.IsNullOrWhiteSpace(row.AccountNumber)
            ? $"IMPORT-{SanitizeNumberSuffix(name)}"
            : row.AccountNumber.Trim();

        return new CreateAccountRequest
        {
            Name = name,
            Number = number,
            Description = BuildDescription(row),
            Balance = MoneyParser.ParseOrDefault(row.CurrentBalanceOwe),
            Limit = MoneyParser.ParseOrDefault(row.CreditLimit),
            AccountOpenDate = ParseDueDate(row.DueDate) ?? DateTime.UtcNow.Date,
            MonthlyPayment = MoneyParser.ParseNullable(row.MonthlyPayment),
            Phone = ImportPhone,
            Email = ImportEmail,
            Url = NormalizeUrl(row.Url),
            Notes = BuildNotes(row),
            IsPaidOff = false,
            IsCreditCard = IsCreditCardCategory(row.Category),
            AccountCategoryId = AccountCategoryMapper.Map(row.Category)
        };
    }

    private static bool? IsCreditCardCategory(string? category) =>
        string.Equals(category?.Trim(), "Credit Card", StringComparison.OrdinalIgnoreCase) ? true : null;

    private static string BuildDescription(MonthlyBillsCsvRow row)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(row.Status))
        {
            parts.Add($"Status: {row.Status.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(row.DueDay))
        {
            parts.Add($"Due day: {row.DueDay.Trim()}");
        }

        return parts.Count == 0 ? "Imported from MonthlyPayments" : string.Join("; ", parts);
    }

    private static string? BuildNotes(MonthlyBillsCsvRow row)
    {
        var sb = new StringBuilder();
        AppendNote(sb, row.Notes);
        AppendNote(sb, row.PaidWith is null ? null : $"Paid with: {row.PaidWith}");
        AppendNote(sb, row.MonthlyDueOverride is null ? null : $"Due override: {row.MonthlyDueOverride}");
        AppendNote(sb, row.PaymentDateExtra is null ? null : $"Payment date: {row.PaymentDateExtra}");
        AppendNote(sb, row.Interest is null ? null : $"Interest: {row.Interest}%");
        AppendNote(sb, row.LastUpdate is null ? null : $"Last update: {row.LastUpdate}");
        AppendNote(sb, row.AvailableCredit is null ? null : $"Available credit: {row.AvailableCredit}");

        var text = sb.ToString().Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    private static void AppendNote(StringBuilder sb, string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        if (sb.Length > 0)
        {
            sb.AppendLine();
        }

        sb.Append(line.Trim());
    }

    private static DateTime? ParseDueDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
        {
            return DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc);
        }

        return null;
    }

    private static string NormalizeUrl(string? url)
    {
        var trimmed = url?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed)
            && trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return DefaultUrl;
    }

    private static string SanitizeNumberSuffix(string name)
    {
        var chars = name.Where(char.IsLetterOrDigit).Take(20).ToArray();
        return chars.Length == 0 ? Guid.NewGuid().ToString("N")[..8] : new string(chars);
    }
}
