using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace CLS.Budget.Import.PaymentSources;

internal static class PaymentSourceCsvReader
{
    public static IReadOnlyList<PaymentSourceCsvRow> Read(string filePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<PaymentSourceCsvRowMap>();

        return csv.GetRecords<PaymentSourceCsvRow>()
            .Where(r => !string.IsNullOrWhiteSpace(r.Name))
            .ToList();
    }

    private sealed class PaymentSourceCsvRowMap : ClassMap<PaymentSourceCsvRow>
    {
        public PaymentSourceCsvRowMap()
        {
            Map(m => m.Name).Name("Name", "Payment Account", "Payment Source");
            Map(m => m.Description).Name("Description");
        }
    }
}
