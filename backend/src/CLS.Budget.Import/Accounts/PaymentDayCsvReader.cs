using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace CLS.Budget.Import.Accounts;

internal static class PaymentDayCsvReader
{
    public static IReadOnlyList<PaymentDayCsvRow> Read(string filePath)
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

        csv.Context.RegisterClassMap<PaymentDayCsvRowMap>();

        return csv.GetRecords<PaymentDayCsvRow>()
            .Where(row => !string.IsNullOrWhiteSpace(row.Name)
                          || !string.IsNullOrWhiteSpace(row.Number))
            .ToList();
    }

    private sealed class PaymentDayCsvRowMap : ClassMap<PaymentDayCsvRow>
    {
        public PaymentDayCsvRowMap()
        {
            Map(m => m.Name).Name("Name", "Account", "Account Name", "Account Bill", "Account / Bill");
            Map(m => m.Number).Name("Number", "Account Number", "Account #");
            Map(m => m.PaymentDate).Name(
                "Payment Date",
                "Payment Day",
                "PaymentDay",
                "Due Day",
                "Due Date");
        }
    }
}
