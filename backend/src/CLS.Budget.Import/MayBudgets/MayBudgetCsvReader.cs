using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CLS.Budget.Import.Accounts;

namespace CLS.Budget.Import.MayBudgets;

internal static class MayBudgetCsvReader
{
    public static IReadOnlyList<MayBudgetCsvRow> Read(string filePath)
    {
        var headerLineIndex = FindHeaderLineIndex(filePath);
        if (headerLineIndex < 0)
        {
            throw new InvalidOperationException(
                "Could not find a header row with \"Account / Bill\" and \"Amount To Pay\".");
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args => CsvHeaderValidator.NormalizeHeaderForMatch(args.Header)
        };

        using var reader = new StreamReader(filePath);
        for (var i = 0; i < headerLineIndex; i++)
        {
            reader.ReadLine();
        }

        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<MayBudgetCsvRowMap>();

        return csv.GetRecords<MayBudgetCsvRow>()
            .Where(IsDataRow)
            .ToList();
    }

    public static int FindHeaderLineNumber(string filePath) => FindHeaderLineIndex(filePath);

    private static int FindHeaderLineIndex(string filePath)
    {
        var lineIndex = 0;
        foreach (var line in File.ReadLines(filePath))
        {
            var lower = line.ToLowerInvariant();
            if (lower.Contains("account / bill", StringComparison.Ordinal) &&
                lower.Contains("amount to pay", StringComparison.Ordinal))
            {
                return lineIndex;
            }

            lineIndex++;
        }

        return -1;
    }

    private static bool IsDataRow(MayBudgetCsvRow row)
    {
        var name = row.AccountBill?.Trim();
        return !string.IsNullOrWhiteSpace(name);
    }

    private sealed class MayBudgetCsvRowMap : ClassMap<MayBudgetCsvRow>
    {
        public MayBudgetCsvRowMap()
        {
            Map(m => m.Active).Name("Active");
            Map(m => m.Month).Name("Month");
            Map(m => m.AccountBill).Name("Account Bill", "Account / Bill");
            Map(m => m.Category).Name("Category");
            Map(m => m.DueDay).Name("Due Day");
            Map(m => m.ApproxAmount).Name("Approx Amount");
            Map(m => m.MonthlyDueOverride).Name("Monthly Due Override");
            Map(m => m.AmountToPay).Name("Amount To Pay");
            Map(m => m.AmountLeft).Name("Amount Left");
            Map(m => m.DatePaid).Name("Date Paid");
            Map(m => m.PaymentAccount).Name("Payment Account");
            Map(m => m.Status).Name("Status");
            Map(m => m.Notes).Name("Notes");
        }
    }
}
