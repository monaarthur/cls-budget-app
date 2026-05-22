using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace CLS.Budget.Import.Accounts;

internal static class MonthlyBillsCsvReader
{
    private static readonly string[] SkipPrefixes =
    [
        "accounts updated",
        "imported from"
    ];

    public static IReadOnlyList<MonthlyBillsCsvRow> Read(string filePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args => CsvHeaderValidator.NormalizeHeaderForMatch(args.Header)
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        csv.Context.RegisterClassMap<MonthlyBillsCsvRowMap>();

        return csv.GetRecords<MonthlyBillsCsvRow>()
            .Where(IsDataRow)
            .ToList();
    }

    private static bool IsDataRow(MonthlyBillsCsvRow row)
    {
        var name = row.AccountBill?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var lower = name.ToLowerInvariant();
        if (SkipPrefixes.Any(p => lower.StartsWith(p, StringComparison.Ordinal)))
        {
            return false;
        }

        return true;
    }

    private sealed class MonthlyBillsCsvRowMap : ClassMap<MonthlyBillsCsvRow>
    {
        public MonthlyBillsCsvRowMap()
        {
            Map(m => m.Active).Name("Active");
            Map(m => m.Category).Name("Category");
            Map(m => m.AccountBill).Name("Account Bill", "Account / Bill");
            Map(m => m.AccountNumber).Name("Account Number", "Account #");
            Map(m => m.DueDate).Name("Due Date");
            Map(m => m.DueDay).Name("Due Day");
            Map(m => m.CurrentBalanceOwe).Name("Current Balance Owe", "Current Balance / Owe");
            Map(m => m.MonthlyPayment).Name("Monthly Payment");
            Map(m => m.PaidWith).Name("Paid With");
            Map(m => m.MonthlyDueOverride).Name("Monthly Due Override");
            Map(m => m.PaymentDateExtra).Name("Payment Date Extra", "Payment Date / Extra");
            Map(m => m.Status).Name("Status");
            Map(m => m.Notes).Name("Notes");
            Map(m => m.Interest).Name("Interest");
            Map(m => m.LastUpdate).Name("LastUpdate", "Last Update");
            Map(m => m.AvailableCredit).Name("Available Credit");
            Map(m => m.CreditLimit).Name("Credit Limit");
            Map(m => m.Url).Name("Url", "URL");
        }
    }
}
