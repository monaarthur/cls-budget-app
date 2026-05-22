using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Accounts.Dtos;
using CLS.Budget.Application.Accounts.Validators;
using FluentValidation;

namespace CLS.Budget.Import.Accounts;

internal sealed class AccountCsvImporter(
    IAccountService accountService,
    CreateAccountRequestValidator validator)
{
    public async Task<ImportSummary> ImportAsync(
        string csvPath,
        bool activeOnly,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        var rows = MonthlyBillsCsvReader.Read(csvPath);
        var summary = new ImportSummary();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var line = i + 2; // header + 1-based

            if (activeOnly && !IsActive(row.Active))
            {
                summary.Skipped++;
                continue;
            }

            var request = MonthlyBillsRowMapper.ToCreateRequest(row);
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                summary.Failed++;
                summary.Messages.Add($"Line {line} ({request.Name}): {string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))}");
                continue;
            }

            if (dryRun)
            {
                summary.Imported++;
                continue;
            }

            var result = await accountService.CreateAsync(request, cancellationToken);
            if (result.Success)
            {
                summary.Imported++;
            }
            else
            {
                summary.Failed++;
                summary.Messages.Add($"Line {line} ({request.Name}): {string.Join("; ", result.Errors)}");
            }
        }

        return summary;
    }

    private static bool IsActive(string? active) =>
        string.Equals(active?.Trim(), "Yes", StringComparison.OrdinalIgnoreCase);
}

internal sealed class ImportSummary
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<string> Messages { get; } = [];
}
