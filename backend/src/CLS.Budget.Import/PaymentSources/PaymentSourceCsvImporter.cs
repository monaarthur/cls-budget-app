using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Import.PaymentSources;

internal sealed class PaymentSourceCsvImporter(IPaymentSourceRepository paymentSourceRepository)
{
    public async Task<ImportSummary> ImportAsync(
        string csvPath,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        var rows = PaymentSourceCsvReader.Read(csvPath);
        var summary = new ImportSummary();
        var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var line = i + 2;
            var name = row.Name!.Trim();

            if (!seenInFile.Add(name))
            {
                summary.Skipped++;
                summary.Messages.Add($"Line {line} ({name}): duplicate in file, skipped.");
                continue;
            }

            var existing = await paymentSourceRepository.GetByNameAsync(name, cancellationToken);
            if (existing is not null)
            {
                summary.Skipped++;
                summary.Messages.Add($"Line {line} ({name}): already exists in database, skipped.");
                continue;
            }

            if (dryRun)
            {
                summary.Imported++;
                continue;
            }

            var entity = new PaymentSource
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(row.Description) ? null : row.Description.Trim()
            };

            await paymentSourceRepository.AddAsync(entity, cancellationToken);
            summary.Imported++;
        }

        return summary;
    }
}

internal sealed class ImportSummary
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<string> Messages { get; } = [];
}
